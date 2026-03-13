using ControlHub.Application.AuditAI.Interfaces.V3;
using ControlHub.Application.AuditAI.Interfaces.V3.Agentic;
using ControlHub.Application.AuditAI.Interfaces.V3.Observability;
using ControlHub.Application.AuditAI.Interfaces.V3.RAG;
using ControlHub.Application.AuditAI.Interfaces.V3.Reasoning;
using Microsoft.Extensions.Logging;

namespace ControlHub.Infrastructure.AI.V3.Agentic.Nodes
{
    public class ExecutorNode : IAgentNode
    {
        private readonly IAgenticRAG _agenticRag;
        private readonly IReasoningModel _reasoningModel;
        private readonly ISystemKnowledgeProvider _knowledgeProvider;
        private readonly IAgentObserver? _observer;
        private readonly ILogger<ExecutorNode> _logger;

        public string Name => "Executor";
        public string Description => "Executes plan steps using available tools";

        public ExecutorNode(
            IAgenticRAG agenticRag,
            IReasoningModel reasoningModel,
            ISystemKnowledgeProvider knowledgeProvider,
            IAgentObserver? observer,
            ILogger<ExecutorNode> logger)
        {
            _agenticRag = agenticRag;
            _reasoningModel = reasoningModel;
            _knowledgeProvider = knowledgeProvider;
            _observer = observer;
            _logger = logger;
        }

        public async Task<IAgentState> ExecuteAsync(IAgentState state, CancellationToken ct = default)
        {
            var clone = (AgentState)state.Clone();

            var plan = clone.GetContext<List<string>>("plan");
            var currentStep = clone.GetContextValue("current_step", 0);

            if (plan == null || !plan.Any())
            {
                clone.Error = "No plan found. Planner node must run first.";
                clone.IsComplete = true;
                return clone;
            }

            if (currentStep > 0)
            {
                _logger.LogInformation("Batch execution already completed. Skipping Executor loop.");
                clone.Context["execution_complete"] = true;
                return clone;
            }

            _logger.LogInformation("Starting BATCH execution for {Count} steps", plan.Count);

            var correlationId = clone.GetContext<string>("correlationId");
            var originalQuery = clone.GetContext<string>("query") ?? "IT Investigation";

            var preRetrievedDocs = clone.GetContext<List<RankedDocument>>("pre_retrieval_docs");
            List<RankedDocument> evidence;

            if (preRetrievedDocs != null && preRetrievedDocs.Any())
            {
                _logger.LogInformation("Executor: Using {Count} pre-retrieved evidence documents", preRetrievedDocs.Count);
                evidence = preRetrievedDocs;
            }
            else
            {
                _logger.LogInformation("Executor: No pre-retrieval docs found, performing batch retrieval");
                var ragOptions = new AgenticRAGOptions(CorrelationId: correlationId);
                var ragResult = await _agenticRag.RetrieveAsync(originalQuery, ragOptions, ct);
                evidence = ragResult.Documents;
            }

            var evidenceSection = "";
            var ragMetadata = clone.GetContext<Dictionary<string, object>>("rag_metadata");
            if (ragMetadata != null && ragMetadata.TryGetValue("evidence_metadata", out var metaObj) && metaObj is LogMetadata logMeta)
            {
                var sb = new System.Text.StringBuilder();
                sb.AppendLine("## Auto-Extracted Evidence (code-extracted, use as ground truth):");
                if (logMeta.AffectedEndpoint != null)
                    sb.AppendLine($"- Request: {logMeta.AffectedEndpoint} → HTTP {logMeta.HttpStatusCode ?? "N/A"}");
                if (logMeta.ErrorCode != null)
                    sb.AppendLine($"- Error Code: {logMeta.ErrorCode}");
                if (logMeta.ErrorMessage != null)
                    sb.AppendLine($"- Error Message: {logMeta.ErrorMessage}");
                if (logMeta.TimestampRange != null)
                    sb.AppendLine($"- Timeline: {logMeta.TimestampRange}");
                sb.AppendLine($"- Severity: {logMeta.ErrorCount} ERROR, {logMeta.WarningCount} WARNING, {logMeta.InfoCount} INFO");
                sb.AppendLine();
                sb.AppendLine("IMPORTANT: Use EXACTLY these values in your diagnosis. Do NOT hallucinate different endpoints or error codes.");
                evidenceSection = sb.ToString();

                if (logMeta.ErrorCode != null)
                {
                    var knowledge = _knowledgeProvider.GetKnowledgeForErrorCode(logMeta.ErrorCode);
                    if (!string.IsNullOrEmpty(knowledge))
                        evidenceSection += "\n" + knowledge;
                }
            }

            var planText = string.Join("\n", plan.Select((s, i) => $"{i + 1}. {s}"));
            var batchPrompt =
                $"You are a senior IT auditor executing an investigation.\n\n" +
                $"## Original Query:\n{originalQuery}\n\n" +
                (string.IsNullOrEmpty(evidenceSection) ? "" : $"{evidenceSection}\n") +
                $"## Investigation Plan:\n{planText}\n\n" +
                $"## Your Task:\n" +
                $"Before writing your final answer, trace the causal chain step by step:\n" +
                $"  a) What was the first observable symptom? (identify the highest-severity log entry)\n" +
                $"  b) What triggered it? (look for WARNINGs or state changes that preceded the failure)\n" +
                $"  c) What underlying condition caused the trigger?\n\n" +
                $"Then produce your FINAL DIAGNOSIS using these JSON fields:\n" +
                $"- 'solution' (your problem summary): One sentence — exact error code, HTTP status, and affected endpoint.\n" +
                $"- 'explanation' (your root cause): Causal chain in the form \"Trigger: X → Effect: Y → Terminal failure: Z\".\n" +
                $"  Quote specific ERROR/WARNING log entries (with timestamps and log levels) that prove each link.\n" +
                $"  Do NOT cite INFO-level entries as causes.\n" +
                $"- 'steps' (your recommendations): Numbered fix actions. Reference the matching runbook if available.\n" +
                $"- 'confidence': Your confidence in the diagnosis (0.0–1.0) based on evidence quality.\n\n" +
                $"CONSTRAINT: Only use error codes, endpoints, and status codes from the Auto-Extracted Evidence above.";

            var batchContext = new ReasoningContext(Query: batchPrompt, RetrievedDocs: evidence);
            var analysis = await _reasoningModel.ReasonAsync(
                batchContext,
                new ReasoningOptions(Temperature: 0.2f, MaxTokens: 6144),
                ct
            );

            var executionResults = new List<string>();

            if (!string.IsNullOrEmpty(analysis.Solution) && analysis.Solution != "Partial Diagnosis")
            {
                executionResults.Add($"## Problem Summary\n{analysis.Solution}");

                if (!string.IsNullOrEmpty(analysis.Explanation) && analysis.Explanation != "Refer to raw response")
                    executionResults.Add($"## Root Cause Analysis\n{analysis.Explanation}");

                if (analysis.Steps.Any())
                {
                    var stepsText = string.Join("\n", analysis.Steps.Select(s => $"- {s}"));
                    executionResults.Add($"## Recommendation\n{stepsText}");
                }
            }
            else
            {
                _logger.LogWarning("LLM did not return structured diagnosis, using step-based fallback");
                foreach (var step in analysis.Steps)
                    executionResults.Add(step);

                if (!executionResults.Any())
                    executionResults.Add($"Investigation completed but no structured findings were returned.\n_(Source: {evidence.Count} logs)_");
            }

            clone.Context["execution_results"] = executionResults;
            clone.Context["current_step"] = plan.Count;
            clone.Context["execution_complete"] = true;
            clone.Context["diagnosis_solution"] = analysis.Solution;
            clone.Context["diagnosis_explanation"] = analysis.Explanation;

            clone.Messages.Add(new AgentMessage(
                "assistant",
                $"Diagnosis complete: {(analysis.Solution.Length > 100 ? analysis.Solution[..100] : analysis.Solution)}...",
                "Executor"
            ));

            return clone;
        }
    }
}
