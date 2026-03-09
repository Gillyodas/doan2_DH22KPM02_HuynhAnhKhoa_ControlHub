using ControlHub.Application.AuditAI.Interfaces.V3.Agentic;
using ControlHub.Application.AuditAI.Interfaces.V3.Observability;
using ControlHub.Application.AuditAI.Interfaces.V3.Reasoning;
using Microsoft.Extensions.Logging;

namespace ControlHub.Infrastructure.AI.V3.Agentic.Nodes
{
    public class VerifierNode : IAgentNode
    {
        private readonly IConfidenceScorer _confidenceScorer;
        private readonly IAgentObserver? _observer;
        private readonly ILogger<VerifierNode> _logger;

        public string Name => "Verifier";
        public string Description => "Verifies execution results and scores confidence";

        public VerifierNode(IConfidenceScorer confidenceScorer, IAgentObserver? observer, ILogger<VerifierNode> logger)
        {
            _confidenceScorer = confidenceScorer;
            _observer = observer;
            _logger = logger;
        }

        public async Task<IAgentState> ExecuteAsync(IAgentState state, CancellationToken ct = default)
        {
            var clone = (AgentState)state.Clone();

            var executionResults = clone.GetContext<List<string>>("execution_results");
            var query = clone.GetContext<string>("query") ?? "";
            var correlationId = clone.GetContext<string>("correlationId");
            var plan = clone.GetContext<List<string>>("plan");

            if (executionResults == null || !executionResults.Any())
            {
                var isGeneralQuery = string.IsNullOrEmpty(correlationId);
                var hasValidPlan = plan != null && plan.Count > 0;

                if (isGeneralQuery && hasValidPlan)
                {
                    clone.Context["verification_passed"] = true;
                    clone.Context["verification_score"] = 0.5f;
                    clone.Context["verification_reason"] = "General knowledge answer (no evidence required)";
                    _logger.LogInformation("Verification PASSED: General query with valid plan");
                    clone.Messages.Add(new AgentMessage("assistant", "Verification PASSED: Medium confidence (0.5) - General knowledge answer"));
                    return clone;
                }
                else
                {
                    clone.Context["verification_passed"] = false;
                    clone.Context["verification_score"] = 0f;
                    clone.Context["verification_reason"] = "No execution results to verify";
                    _logger.LogWarning("Verification FAILED: No execution results for incident query");
                    return clone;
                }
            }

            var preRetrievedDocs = clone.GetContext<List<Application.AuditAI.Interfaces.V3.RAG.RankedDocument>>("pre_retrieval_docs");
            var totalRetrievedDocs = preRetrievedDocs?.Count ?? 0;
            var hasRetrievedDocs = totalRetrievedDocs > 0;

            _logger.LogInformation("Structured Verification: totalRetrievedDocs = {Count}, correlationId = {Id}",
                totalRetrievedDocs, correlationId ?? "null");

            if (!hasRetrievedDocs && !string.IsNullOrEmpty(correlationId))
            {
                _logger.LogWarning("Verification FAILED: No documents retrieved for correlationId: {Id}", correlationId);
                clone.Context["verification_passed"] = false;
                clone.Context["verification_score"] = 0f;
                clone.Context["verification_reason"] = $"No logs found for correlationId: {correlationId}";
                clone.Messages.Add(new AgentMessage("assistant", $"Verification FAILED: No logs found for correlationId {correlationId}"));
                return clone;
            }

            if (hasRetrievedDocs)
            {
                var confidence = Math.Min(0.5f + (totalRetrievedDocs * 0.05f), 0.95f);
                clone.Context["verification_passed"] = true;
                clone.Context["verification_score"] = confidence;
                clone.Context["verification_reason"] = $"Found {totalRetrievedDocs} relevant documents";
                clone.Messages.Add(new AgentMessage("assistant", $"Verification PASSED: {confidence:P0} confidence ({totalRetrievedDocs} docs retrieved)"));
                _logger.LogInformation("Verification PASSED: {Score:F2} ({Docs} docs)", confidence, totalRetrievedDocs);
                return clone;
            }

            var reasoningResult = new ReasoningResult(
                Solution: string.Join("\n", executionResults),
                Explanation: "Verification of execution results",
                Steps: executionResults,
                Confidence: 0.7f
            );

            var mockDocs = executionResults.Select((r, i) => new Application.AuditAI.Interfaces.V3.RAG.RankedDocument(
                Content: r,
                RelevanceScore: 0.8f,
                Metadata: new Dictionary<string, string> { ["source"] = "execution" }
            )).ToList();

            var scorerContext = new ReasoningContext(Query: query, RetrievedDocs: mockDocs);
            var score = await _confidenceScorer.ScoreAsync(reasoningResult, scorerContext, ct);
            var passed = score.IsConfident(0.5f);

            clone.Context["verification_passed"] = passed;
            clone.Context["verification_score"] = score.Overall;
            clone.Context["verification_reason"] = score.Justification;
            clone.Messages.Add(new AgentMessage("assistant",
                $"Verification {(passed ? "PASSED" : "FAILED")}: {score.GetLevel()} confidence ({score.Overall:P0})"));

            _logger.LogInformation("Verification {Result}: {Score:F2}", passed ? "PASSED" : "FAILED", score.Overall);
            return clone;
        }
    }
}
