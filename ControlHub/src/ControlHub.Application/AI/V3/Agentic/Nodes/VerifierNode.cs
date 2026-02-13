using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ControlHub.Application.Common.Interfaces.AI.V3.Agentic;
using ControlHub.Application.Common.Interfaces.AI.V3.Reasoning;
using ControlHub.Application.Common.Interfaces.AI.V3.Observability;
using Microsoft.Extensions.Logging;

namespace ControlHub.Application.AI.V3.Agentic.Nodes
{
    /// <summary>
    /// VerifierNode - Verify execution results and determine completeness.
    /// </summary>
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

            // Get execution results and context
            var executionResults = clone.GetContext<List<string>>("execution_results");
            var query = clone.GetContext<string>("query") ?? "";
            var correlationId = clone.GetContext<string>("correlationId");
            var plan = clone.GetContext<List<string>>("plan");

            // Case 1: No execution results at all
            if (executionResults == null || !executionResults.Any())
            {
                // Check if this is a general query (no correlationId) with a valid plan
                var isGeneralQuery = string.IsNullOrEmpty(correlationId);
                var hasValidPlan = plan != null && plan.Count > 0;

                if (isGeneralQuery && hasValidPlan)
                {
                    // Accept general knowledge answers
                    clone.Context["verification_passed"] = true;
                    clone.Context["verification_score"] = 0.5f;
                    clone.Context["verification_reason"] = "General knowledge answer (no evidence required)";
                    
                    _logger.LogInformation("Verification PASSED: General query with valid plan");
                    
                    clone.Messages.Add(new AgentMessage(
                        "assistant",
                        "Verification PASSED: Medium confidence (0.5) - General knowledge answer"
                    ));
                    
                    return clone;
                }
                else
                {
                    // Incident query but no results → FAIL
                    clone.Context["verification_passed"] = false;
                    clone.Context["verification_score"] = 0f;
                    clone.Context["verification_reason"] = "No execution results to verify";
                    
                    _logger.LogWarning("Verification FAILED: No execution results for incident query");
                    
                    return clone;
                }
            }

            // Case 2: Has execution results - check if docs were retrieved (Phase 6: Structured Verification)
            var preRetrievedDocs = clone.GetContext<List<Common.Interfaces.AI.V3.RAG.RankedDocument>>("pre_retrieval_docs");
            var totalRetrievedDocs = preRetrievedDocs?.Count ?? 0;
            var hasRetrievedDocs = totalRetrievedDocs > 0;
            
            _logger.LogInformation("Structured Verification: totalRetrievedDocs = {Count}, correlationId = {Id}", 
                totalRetrievedDocs, correlationId ?? "null");

            // Only fail if we have a correlationId but NO docs were retrieved
            if (!hasRetrievedDocs && !string.IsNullOrEmpty(correlationId))
            {
                _logger.LogWarning("Verification FAILED: No documents retrieved for correlationId: {Id}", correlationId);
                
                clone.Context["verification_passed"] = false;
                clone.Context["verification_score"] = 0f;
                clone.Context["verification_reason"] = $"No logs found for correlationId: {correlationId}";
                
                clone.Messages.Add(new AgentMessage(
                    "assistant",
                    $"Verification FAILED: No logs found for correlationId {correlationId}"
                ));
                
                return clone;
            }

            // If we have retrieved docs, verification passes with good confidence
            if (hasRetrievedDocs)
            {
                // Calculate confidence based on number of docs retrieved
                var confidence = Math.Min(0.5f + (totalRetrievedDocs * 0.05f), 0.95f);
                
                clone.Context["verification_passed"] = true;
                clone.Context["verification_score"] = confidence;
                clone.Context["verification_reason"] = $"Found {totalRetrievedDocs} relevant documents (Logs/Knowledge)";

                clone.Messages.Add(new AgentMessage(
                    "assistant",
                    $"Verification PASSED: {confidence:P0} confidence ({totalRetrievedDocs} docs retrieved)"
                ));

                _logger.LogInformation("Verification PASSED: {Score:F2} ({Docs} docs)", confidence, totalRetrievedDocs);

                return clone;
            }

            // Fallback: Use ConfidenceScorer for complex verification
            var reasoningResult = new ReasoningResult(
                Solution: string.Join("\n", executionResults),
                Explanation: "Verification of execution results",
                Steps: executionResults,
                Confidence: 0.7f
            );

            // Create mock retrieved docs based on execution results
            var mockDocs = executionResults.Select((r, i) => new Common.Interfaces.AI.V3.RAG.RankedDocument(
                Content: r,
                RelevanceScore: 0.8f,
                Metadata: new Dictionary<string, string> { ["source"] = "execution" }
            )).ToList();

            var context = new ReasoningContext(
                Query: query,
                RetrievedDocs: mockDocs
            );

            var score = await _confidenceScorer.ScoreAsync(reasoningResult, context, ct);

            // Verification passes if confidence is above threshold
            var passed = score.IsConfident(0.5f); // Lower threshold

            clone.Context["verification_passed"] = passed;
            clone.Context["verification_score"] = score.Overall;
            clone.Context["verification_reason"] = score.Justification;

            clone.Messages.Add(new AgentMessage(
                "assistant",
                $"Verification {(passed ? "PASSED" : "FAILED")}: {score.GetLevel()} confidence ({score.Overall:P0})"
            ));

            _logger.LogInformation("Verification {Result}: {Score:F2}", 
                passed ? "PASSED" : "FAILED", score.Overall);

            return clone;
        }
    }
}
