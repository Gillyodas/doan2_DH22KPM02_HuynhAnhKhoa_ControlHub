using ControlHub.Application.Common.Interfaces.AI;
using ControlHub.Application.Common.Interfaces.AI.V3.Parsing;
using ControlHub.Application.Common.Interfaces.AI.V3.RAG;
using ControlHub.Application.Common.Interfaces.AI.V3.Reasoning;
using ControlHub.Application.Common.Logging;
using ControlHub.Application.Common.Logging.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace ControlHub.Application.AI.V3
{
    /// <summary>
    /// V3 Agentic Audit Service - Tích hợp Hybrid Parsing + Enhanced RAG + Reasoning.
    /// Upgrade path từ V2.5 với optional V3 features.
    /// </summary>
    public class AgenticAuditServiceV3 : IAuditAgentService
    {
        private readonly IHybridLogParser _hybridParser;
        private readonly IAgenticRAG _agenticRag;
        private readonly IReasoningModel _reasoningModel;
        private readonly IConfidenceScorer _confidenceScorer;
        private readonly ILogReaderService _logReader;
        private readonly IConfiguration _config;
        private readonly ILogger<AgenticAuditServiceV3> _logger;

        // Feature flags
        private readonly bool _useHybridParsing;
        private readonly bool _useEnhancedRag;
        private readonly bool _useReasoning;

        public AgenticAuditServiceV3(
            IHybridLogParser hybridParser,
            IAgenticRAG agenticRag,
            IReasoningModel reasoningModel,
            IConfidenceScorer confidenceScorer,
            ILogReaderService logReader,
            IConfiguration config,
            ILogger<AgenticAuditServiceV3> logger)
        {
            _hybridParser = hybridParser;
            _agenticRag = agenticRag;
            _reasoningModel = reasoningModel;
            _confidenceScorer = confidenceScorer;
            _logReader = logReader;
            _config = config;
            _logger = logger;

            // Feature flags from config
            _useHybridParsing = config.GetValue("AuditAI:V3:EnableHybridParsing", true);
            _useEnhancedRag = config.GetValue("AuditAI:V3:EnableEnhancedRAG", true);
            _useReasoning = config.GetValue("AuditAI:V3:EnableReasoning", true);
        }

        /// <summary>
        /// V3 Investigation with full pipeline: Parse → RAG → Reason → Score.
        /// </summary>
        public async Task<AuditResult> InvestigateSessionAsync(string correlationId, string lang = "en")
        {
            var toolsUsed = new List<string>();
            var processedTemplates = new List<LogTemplate>();
            var ct = CancellationToken.None;

            try
            {
                _logger.LogInformation("V3 Investigation started for correlationId: {CorrelationId}", correlationId);

                // Step 1: Fetch logs
                var rawLogs = await _logReader.GetLogsByCorrelationIdAsync(correlationId);
                if (!rawLogs.Any())
                {
                    return new AuditResult(
                        Analysis: "No logs found for the given correlation ID.",
                        ProcessedTemplates: processedTemplates,
                        ToolsUsed: toolsUsed
                    );
                }

                toolsUsed.Add("LogReader");

                // Step 2: Parse logs (V3 Hybrid Parsing)
                LogClassificationInfo? classification = null;
                if (_useHybridParsing)
                {
                    foreach (var log in rawLogs.Take(10)) // Process top 10 logs
                    {
                        var parseResult = await _hybridParser.ParseSingleAsync(log.Message, ct);
                        processedTemplates.Add(new LogTemplate(
                            TemplateId: Guid.NewGuid().ToString(),
                            Pattern: parseResult.Classification?.Category ?? parseResult.Template,
                            Count: 1,
                            FirstSeen: DateTime.UtcNow,
                            LastSeen: DateTime.UtcNow,
                            Severity: "Information"
                        ));

                        // Use first parse result for classification
                        if (classification == null && parseResult.Classification != null)
                        {
                            classification = new LogClassificationInfo(
                                parseResult.Classification.Category,
                                parseResult.Classification.SubCategory,
                                parseResult.Confidence
                            );
                        }
                    }
                    toolsUsed.Add("HybridParser");
                    _logger.LogInformation("Parsed {Count} logs: Category={Category}",
                        processedTemplates.Count, classification?.Category ?? "Unknown");
                }

                // Step 3: Build query for RAG
                var query = BuildInvestigationQuery(rawLogs, classification);

                // Step 4: RAG Retrieval (V3 Enhanced RAG)
                List<RankedDocument> retrievedDocs = new();
                if (_useEnhancedRag)
                {
                    var ragResult = await _agenticRag.RetrieveAsync(query, null, ct);
                    retrievedDocs = ragResult.Documents;
                    toolsUsed.Add($"RAG:{ragResult.StrategyUsed}");
                    _logger.LogInformation("RAG retrieved {Count} documents using {Strategy}",
                        retrievedDocs.Count, ragResult.StrategyUsed);
                }

                // Step 5: Reasoning (V3 LLM Reasoning)
                string analysis = "";
                if (_useReasoning && retrievedDocs.Count > 0)
                {
                    var context = new ReasoningContext(query, retrievedDocs, null);
                    var reasoningResult = await _reasoningModel.ReasonAsync(context, null, ct);
                    var confidenceScore = await _confidenceScorer.ScoreAsync(reasoningResult, context, ct);

                    analysis = $"[Confidence: {confidenceScore.GetLevel()}]\n\n" +
                               $"## Solution\n{reasoningResult.Solution}\n\n" +
                               $"## Explanation\n{reasoningResult.Explanation}\n\n" +
                               $"## Steps\n" + string.Join("\n", reasoningResult.Steps.Select((s, i) => $"{i + 1}. {s}"));

                    toolsUsed.Add("ReasoningModel");
                    _logger.LogInformation("Reasoning completed: Confidence={Confidence:F2}",
                        confidenceScore.Overall);
                }
                else
                {
                    // Fallback: simple summary
                    analysis = "Analysis based on log patterns:\n" +
                               string.Join("\n", processedTemplates.Take(5).Select(t => $"- [{t.Severity}] {t.Pattern}"));
                }

                return new AuditResult(
                    Analysis: analysis,
                    ProcessedTemplates: processedTemplates,
                    ToolsUsed: toolsUsed
                );
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "V3 Investigation failed for correlationId: {CorrelationId}", correlationId);
                return new AuditResult(
                    Analysis: $"Investigation failed: {ex.Message}",
                    ProcessedTemplates: processedTemplates,
                    ToolsUsed: toolsUsed
                );
            }
        }

        /// <summary>
        /// V3 Chat with reasoning integration.
        /// </summary>
        public async Task<ChatResult> ChatAsync(ChatRequest request, string lang = "en")
        {
            var toolsUsed = new List<string>();
            var ct = CancellationToken.None;
            int logCount = 0;

            try
            {
                _logger.LogInformation("V3 Chat: {Question}", request.Question);

                // Step 1: Fetch logs if context provided
                var logs = new List<LogEntry>();
                if (!string.IsNullOrWhiteSpace(request.CorrelationId))
                {
                    logs = (await _logReader.GetLogsByCorrelationIdAsync(request.CorrelationId)).ToList();
                }
                else if (request.StartTime.HasValue && request.EndTime.HasValue)
                {
                    logs = (await _logReader.GetLogsByTimeRangeAsync(request.StartTime.Value, request.EndTime.Value)).ToList();
                }
                logCount = logs.Count;
                if (logs.Any()) toolsUsed.Add("LogReader");

                // Step 2: RAG Retrieval
                var retrievedDocs = new List<RankedDocument>();
                if (_useEnhancedRag)
                {
                    var ragResult = await _agenticRag.RetrieveAsync(request.Question, null, ct);
                    retrievedDocs = ragResult.Documents;
                    toolsUsed.Add($"RAG:{ragResult.StrategyUsed}");
                }

                // Step 3: Reasoning
                string answer = "";
                if (_useReasoning)
                {
                    var context = new ReasoningContext(request.Question, retrievedDocs, null);
                    var reasoningResult = await _reasoningModel.ReasonAsync(context, null, ct);
                    var confidenceScore = await _confidenceScorer.ScoreAsync(reasoningResult, context, ct);

                    answer = $"{reasoningResult.Solution}\n\n" +
                             $"_Confidence: {confidenceScore.GetLevel()} ({confidenceScore.Overall:P0})_";

                    toolsUsed.Add("ReasoningModel");
                }
                else
                {
                    answer = "Reasoning is disabled. Please check configuration.";
                }

                return new ChatResult(
                    Answer: answer,
                    LogCount: logCount,
                    ToolsUsed: toolsUsed
                );
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "V3 Chat failed");
                return new ChatResult(
                    Answer: $"Error: {ex.Message}",
                    LogCount: logCount,
                    ToolsUsed: toolsUsed
                );
            }
        }

        private string BuildInvestigationQuery(IEnumerable<LogEntry> logs, LogClassificationInfo? classification)
        {
            var query = new System.Text.StringBuilder();
            query.Append("Investigate the following issue: ");

            if (classification != null)
            {
                query.Append($"[{classification.Category}] ");
            }

            // Add first few log messages for context
            var topLogs = logs.Take(3).Select(l => l.Message);
            query.Append(string.Join(" | ", topLogs));

            return query.ToString();
        }
    }

    // Internal helper record (different from interface's LogClassification)
    internal record LogClassificationInfo(string Category, string SubCategory, float Confidence);
}
