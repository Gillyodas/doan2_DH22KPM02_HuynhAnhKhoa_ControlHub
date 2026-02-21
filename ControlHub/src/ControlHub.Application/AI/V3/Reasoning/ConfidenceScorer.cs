using ControlHub.Application.Common.Interfaces.AI.V3.Reasoning;
using Microsoft.Extensions.Logging;

namespace ControlHub.Application.AI.V3.Reasoning
{
    /// <summary>
    /// Confidence Scorer - Multi-factor scoring cho reasoning results.
    /// Factors: Retrieval quality, Reasoning coherence, Solution completeness.
    /// </summary>
    public class ConfidenceScorer : IConfidenceScorer
    {
        private readonly ILogger<ConfidenceScorer> _logger;

        public ConfidenceScorer(ILogger<ConfidenceScorer> logger)
        {
            _logger = logger;
        }

        public Task<ConfidenceScore> ScoreAsync(
            ReasoningResult result,
            ReasoningContext context,
            CancellationToken ct = default)
        {
            // Factor 1: Retrieval Confidence
            var retrievalConfidence = CalculateRetrievalConfidence(context);

            // Factor 2: Reasoning Confidence
            var reasoningConfidence = CalculateReasoningConfidence(result);

            // Factor 3: Solution Completeness
            var completenessBonus = CalculateCompletenessBonus(result);

            // Weighted average
            var overall = (retrievalConfidence * 0.4f) +
                          (reasoningConfidence * 0.4f) +
                          (completenessBonus * 0.2f);

            // Clamp to [0, 1]
            overall = Math.Clamp(overall, 0f, 1f);

            var justification = GenerateJustification(retrievalConfidence, reasoningConfidence, completenessBonus);

            _logger.LogInformation(
                "Confidence scored: Overall={Overall:F2}, Retrieval={Retrieval:F2}, Reasoning={Reasoning:F2}",
                overall,
                retrievalConfidence,
                reasoningConfidence
            );

            return Task.FromResult(new ConfidenceScore(
                overall,
                retrievalConfidence,
                reasoningConfidence,
                justification
            ));
        }

        /// <summary>
        /// Calculate confidence from RAG retrieval quality.
        /// </summary>
        private float CalculateRetrievalConfidence(ReasoningContext context)
        {
            if (context.RetrievedDocs.Count == 0)
                return 0f;

            // Top-K average score
            var topK = Math.Min(5, context.RetrievedDocs.Count);
            var avgScore = context.RetrievedDocs
                .Take(topK)
                .Average(d => d.RelevanceScore);

            // Number of high-quality docs (score > 0.7)
            var highQualityCount = context.RetrievedDocs.Count(d => d.RelevanceScore > 0.7f);
            var countBonus = Math.Min(highQualityCount * 0.05f, 0.2f); // Max 0.2 bonus

            return Math.Min(avgScore + countBonus, 1f);
        }

        /// <summary>
        /// Calculate confidence from reasoning quality.
        /// </summary>
        private float CalculateReasoningConfidence(ReasoningResult result)
        {
            float confidence = result.Confidence; // Start with LLM's self-reported confidence

            // Adjust based on steps quality
            if (result.Steps.Count >= 3)
            {
                confidence += 0.1f; // Good step-by-step reasoning
            }
            else if (result.Steps.Count == 0)
            {
                confidence -= 0.2f; // No steps = less trustworthy
            }

            // Adjust based on explanation length
            if (result.Explanation.Length > 100)
            {
                confidence += 0.05f; // Detailed explanation
            }

            // Adjust based on solution quality
            if (string.IsNullOrWhiteSpace(result.Solution))
            {
                confidence -= 0.3f; // Empty solution is bad
            }
            else if (result.Solution.Length > 50)
            {
                confidence += 0.05f; // Substantial solution
            }

            return Math.Clamp(confidence, 0f, 1f);
        }

        /// <summary>
        /// Calculate bonus for solution completeness.
        /// </summary>
        private float CalculateCompletenessBonus(ReasoningResult result)
        {
            float bonus = 0f;

            // Has solution
            if (!string.IsNullOrWhiteSpace(result.Solution))
                bonus += 0.3f;

            // Has explanation
            if (!string.IsNullOrWhiteSpace(result.Explanation))
                bonus += 0.3f;

            // Has steps
            if (result.Steps.Count > 0)
                bonus += 0.2f;

            // Has multiple steps (thorough)
            if (result.Steps.Count >= 3)
                bonus += 0.2f;

            return Math.Min(bonus, 1f);
        }

        /// <summary>
        /// Generate human-readable justification.
        /// </summary>
        private string GenerateJustification(float retrievalConf, float reasoningConf, float completeness)
        {
            var parts = new System.Collections.Generic.List<string>();

            if (retrievalConf >= 0.7f)
                parts.Add("High-quality evidence retrieved");
            else if (retrievalConf >= 0.5f)
                parts.Add("Moderate evidence quality");
            else
                parts.Add("Limited evidence available");

            if (reasoningConf >= 0.7f)
                parts.Add("reasoning is coherent");
            else if (reasoningConf >= 0.5f)
                parts.Add("reasoning is acceptable");
            else
                parts.Add("reasoning needs verification");

            if (completeness >= 0.8f)
                parts.Add("solution is complete");
            else if (completeness >= 0.5f)
                parts.Add("solution is partial");
            else
                parts.Add("solution is incomplete");

            return string.Join(", ", parts) + ".";
        }
    }
}
