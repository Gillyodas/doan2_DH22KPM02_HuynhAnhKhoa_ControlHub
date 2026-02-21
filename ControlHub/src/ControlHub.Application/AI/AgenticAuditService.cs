using System.Text;
using ControlHub.Application.Common.Interfaces.AI;
using ControlHub.Application.Common.Logging;
using ControlHub.Application.Common.Logging.Interfaces;

namespace ControlHub.Application.AI
{
    public class AgenticAuditService : IAuditAgentService
    {
        private readonly ILogParserService _parserService;
        private readonly ISamplingStrategy _samplingStrategy;
        private readonly IRunbookService _runbookService;
        private readonly IAIAnalysisService _aiService;
        private readonly ILogReaderService _logReader;
        private readonly Microsoft.Extensions.Configuration.IConfiguration _config;

        public AgenticAuditService(
            ILogParserService parserService,
            ISamplingStrategy samplingStrategy,
            IRunbookService runbookService,
            IAIAnalysisService aiService,
            ILogReaderService logReader,
            Microsoft.Extensions.Configuration.IConfiguration config)
        {
            _parserService = parserService;
            _samplingStrategy = samplingStrategy;
            _runbookService = runbookService;
            _aiService = aiService;
            _logReader = logReader;
            _config = config;
        }

        public async Task<AuditResult> InvestigateSessionAsync(string correlationId, string lang = "en")
        {
            var toolsUsed = new List<string>();

            // 1. Get Raw Logs
            var rawLogs = await _logReader.GetLogsByCorrelationIdAsync(correlationId);
            if (!rawLogs.Any())
            {
                return new AuditResult("No logs found for this session.", new List<LogTemplate>(), toolsUsed);
            }

            // 2. Parse & Sample (Pre-computation)
            var parseResult = await _parserService.ParseLogsAsync(rawLogs);
            var parsedTemplates = parseResult.Templates;
            var sampledTemplates = _samplingStrategy.Sample(parsedTemplates, 50);

            // 3. Chain of Thought (ReAct Loop)
            // Simplified Agent:
            // Step 1: Look for Runbooks for top error templates
            // Step 2: Build Context
            // Step 3: Ask LLM

            toolsUsed.Add("Drain3Parser");
            toolsUsed.Add("WeightedReservoirSampling");

            // 3.1 Fetch Runbooks for Errors
            var errorTemplates = sampledTemplates.Where(t => t.Severity == "Error" || t.Severity == "Fatal").ToList();
            var runbookContext = new StringBuilder();

            if (errorTemplates.Any())
            {
                toolsUsed.Add("RunbookLookup");
                foreach (var tmpl in errorTemplates.Take(3)) // Limit runbook lookups
                {
                    var runbooks = await _runbookService.FindRelatedRunbooksAsync(tmpl.Pattern);
                    if (runbooks.Any())
                    {
                        runbookContext.AppendLine($"\n[Runbook for pattern: {tmpl.Pattern}]");
                        foreach (var rb in runbooks)
                        {
                            runbookContext.AppendLine($"- Possible Cause: {rb.Problem}");
                            runbookContext.AppendLine($"  Solution: {rb.Solution}");
                        }
                    }
                }
            }

            // 3.2 Build Prompt
            var prompt = BuildPrompt(sampledTemplates, runbookContext.ToString(), lang);

            // 3.3 Inference
            var aiResponse = await _aiService.AnalyzeLogsAsync(prompt);

            return new AuditResult(aiResponse, parsedTemplates, toolsUsed);
        }

        public async Task<ChatResult> ChatAsync(ChatRequest request, string lang = "en")
        {
            var toolsUsed = new List<string>();

            // ─────────────────────────────────────────────────────────
            // Step 1: Fetch Logs
            // ─────────────────────────────────────────────────────────
            var rawLogs = await FetchLogsForChatAsync(request);

            if (!rawLogs.Any())
            {
                return new ChatResult(
                    "No logs found for the specified criteria.",
                    0,
                    toolsUsed
                );
            }

            // ─────────────────────────────────────────────────────────
            // Step 2: Parse & Sample
            // ─────────────────────────────────────────────────────────
            var parseResult = await _parserService.ParseLogsAsync(rawLogs);
            var sampledTemplates = _samplingStrategy.Sample(parseResult.Templates, 30);

            toolsUsed.Add("Drain3Parser");
            toolsUsed.Add("WeightedReservoirSampling");

            // ─────────────────────────────────────────────────────────
            // Step 3: Runbook Lookup for Error Templates
            // ─────────────────────────────────────────────────────────
            var errorTemplates = sampledTemplates
                .Where(t => t.Severity == "Error" || t.Severity == "Fatal")
                .Take(3)
                .ToList();

            var runbookContext = new StringBuilder();

            if (errorTemplates.Any())
            {
                toolsUsed.Add("RunbookLookup");

                foreach (var tmpl in errorTemplates)
                {
                    var runbooks = await _runbookService.FindRelatedRunbooksAsync(tmpl.Pattern);

                    foreach (var rb in runbooks)
                    {
                        runbookContext.AppendLine($"[Pattern: {tmpl.Pattern}]");
                        runbookContext.AppendLine($"  Problem: {rb.Problem}");
                        runbookContext.AppendLine($"  Solution: {rb.Solution}");
                    }
                }
            }

            // ─────────────────────────────────────────────────────────
            // Step 4: Build Prompt & Call LLM
            // ─────────────────────────────────────────────────────────
            var prompt = BuildChatPromptV2(
                sampledTemplates,
                runbookContext.ToString(),
                request.Question,
                lang
            );

            var aiResponse = await _aiService.AnalyzeLogsAsync(prompt);

            return new ChatResult(aiResponse, rawLogs.Count, toolsUsed);
        }

        /// <summary>
        /// Fetches logs based on ChatRequest (prioritizes CorrelationId over TimeRange).
        /// </summary>
        private async Task<List<LogEntry>> FetchLogsForChatAsync(ChatRequest request)
        {
            // Priority 1: If CorrelationId is provided, use it
            if (!string.IsNullOrEmpty(request.CorrelationId))
            {
                return await _logReader.GetLogsByCorrelationIdAsync(request.CorrelationId);
            }

            // Priority 2: Use TimeRange
            var endTime = request.EndTime ?? DateTime.UtcNow;
            var startTime = request.StartTime ?? endTime.AddHours(-24);

            return await _logReader.GetLogsByTimeRangeAsync(startTime, endTime);
        }

        /// <summary>
        /// Builds the V2.5 chat prompt with templates and runbook context.
        /// </summary>
        private string BuildChatPromptV2(
            List<LogTemplate> templates,
            string runbookContext,
            string question,
            string lang)
        {
            var sb = new StringBuilder();

            // Language mapping
            string languageName = lang.ToLower() switch
            {
                "vi" or "vn" => "Vietnamese",
                _ => "English"
            };

            // System instruction
            sb.AppendLine("You are an expert SRE assistant.");
            sb.AppendLine($"Task: Answer the user's question based on log data. Respond in {languageName}.");

            // Runbook context (if any)
            if (!string.IsNullOrEmpty(runbookContext))
            {
                sb.AppendLine("\n=== KNOWLEDGE BASE ===");
                sb.AppendLine(runbookContext);
            }

            // Log summary
            sb.AppendLine("\n=== LOG SUMMARY ===");
            sb.AppendLine("Format: [Severity] [Count] Template");
            foreach (var t in templates)
            {
                sb.AppendLine($"[{t.Severity}] [x{t.Count}] {t.Pattern}");
            }

            // User question
            sb.AppendLine("\n=== USER QUESTION ===");
            sb.AppendLine(question);

            sb.AppendLine("\n=== INSTRUCTIONS ===");
            sb.AppendLine("1. Focus on answering the specific question.");
            sb.AppendLine("2. Reference relevant log patterns.");
            sb.AppendLine("3. Suggest actionable next steps if applicable.");

            return sb.ToString();
        }


        private string BuildPrompt(List<LogTemplate> templates, string runbookContext, string lang)
        {
            var sb = new StringBuilder();

            // Map Language
            string languageName = lang.ToLower() switch
            {
                "vi" => "Vietnamese",
                "vn" => "Vietnamese",
                _ => "English"
            };

            sb.AppendLine("You are an expert Reliability Engineer (SRE).");
            sb.AppendLine($"Task: Analyze the log summary below and discover the root cause. Response in {languageName}.");
            sb.AppendLine("Use the provided Runbook knowledge if applicable.");

            if (!string.IsNullOrEmpty(runbookContext))
            {
                sb.AppendLine("\n=== KNOWLEDGE BASE / RUNBOOKS ===");
                sb.AppendLine(runbookContext);
            }

            sb.AppendLine("\n=== LOG SESSION SUMMARY ===");
            sb.AppendLine("Format: [Severity] [Count] [TimeRange] Template");

            foreach (var t in templates)
            {
                sb.AppendLine($"[{t.Severity}] [x{t.Count}] [{t.FirstSeen:HH:mm:ss}-{t.LastSeen:HH:mm:ss}] {t.Pattern}");
            }

            sb.AppendLine("\n=== INSTRUCTIONS ===");
            sb.AppendLine("1. Identify the primary error.");
            sb.AppendLine("2. specific Log Patterns/Sequences that lead to the error.");
            sb.AppendLine("3. Suggest a concrete fix based on Runbooks or General Knowledge.");

            return sb.ToString();
        }
    }
}
