using ControlHub.Application.AuditAI.Interfaces.V3.Reasoning;
using Microsoft.Extensions.Logging;

namespace ControlHub.Infrastructure.AI.V3.Reasoning
{
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
            var retrievalConfidence = CalculateRetrievalConfidence(context);
            var reasoningConfidence = CalculateReasoningConfidence(result);
            var completenessBonus = CalculateCompletenessBonus(result);

            var overall = Math.Clamp(
                (retrievalConfidence * 0.4f) + (reasoningConfidence * 0.4f) + (completenessBonus * 0.2f),
                0f, 1f);

            var justification = GenerateJustification(retrievalConfidence, reasoningConfidence, completenessBonus);

            _logger.LogInformation(
                "Confidence scored: Overall={Overall:F2}, Retrieval={Retrieval:F2}, Reasoning={Reasoning:F2}",
                overall, retrievalConfidence, reasoningConfidence);

            return Task.FromResult(new ConfidenceScore(overall, retrievalConfidence, reasoningConfidence, justification));
        }

        private float CalculateRetrievalConfidence(ReasoningContext context)
        {
            if (context.RetrievedDocs.Count == 0) return 0f;
            var topK = Math.Min(5, context.RetrievedDocs.Count);
            var avgScore = context.RetrievedDocs.Take(topK).Average(d => d.RelevanceScore);
            var countBonus = Math.Min(context.RetrievedDocs.Count(d => d.RelevanceScore > 0.7f) * 0.05f, 0.2f);
            return Math.Min(avgScore + countBonus, 1f);
        }

        private float CalculateReasoningConfidence(ReasoningResult result)
        {
            var confidence = result.Confidence;
            if (result.Steps.Count >= 3) confidence += 0.1f;
            else if (result.Steps.Count == 0) confidence -= 0.2f;
            if (result.Explanation.Length > 100) confidence += 0.05f;
            if (string.IsNullOrWhiteSpace(result.Solution)) confidence -= 0.3f;
            else if (result.Solution.Length > 50) confidence += 0.05f;
            return Math.Clamp(confidence, 0f, 1f);
        }

        private float CalculateCompletenessBonus(ReasoningResult result)
        {
            var bonus = 0f;
            if (!string.IsNullOrWhiteSpace(result.Solution)) bonus += 0.3f;
            if (!string.IsNullOrWhiteSpace(result.Explanation)) bonus += 0.3f;
            if (result.Steps.Count > 0) bonus += 0.2f;
            if (result.Steps.Count >= 3) bonus += 0.2f;
            return Math.Min(bonus, 1f);
        }

        private string GenerateJustification(float retrievalConf, float reasoningConf, float completeness)
        {
            var parts = new List<string>();
            parts.Add(retrievalConf >= 0.7f ? "High-quality evidence retrieved" :
                      retrievalConf >= 0.5f ? "Moderate evidence quality" : "Limited evidence available");
            parts.Add(reasoningConf >= 0.7f ? "reasoning is coherent" :
                      reasoningConf >= 0.5f ? "reasoning is acceptable" : "reasoning needs verification");
            parts.Add(completeness >= 0.8f ? "solution is complete" :
                      completeness >= 0.5f ? "solution is partial" : "solution is incomplete");
            return string.Join(", ", parts) + ".";
        }
    }
}
