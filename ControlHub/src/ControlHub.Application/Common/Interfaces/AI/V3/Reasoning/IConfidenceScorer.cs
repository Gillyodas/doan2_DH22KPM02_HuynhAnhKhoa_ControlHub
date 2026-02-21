namespace ControlHub.Application.Common.Interfaces.AI.V3.Reasoning
{
    /// <summary>
    /// Confidence Scorer interface - Đánh giá độ tin cậy của reasoning result.
    /// Multi-factor scoring: retrieval quality + reasoning coherence.
    /// </summary>
    public interface IConfidenceScorer
    {
        /// <summary>
        /// Score confidence cho reasoning result.
        /// </summary>
        Task<ConfidenceScore> ScoreAsync(
            ReasoningResult result,
            ReasoningContext context,
            CancellationToken ct = default
        );
    }

    /// <summary>
    /// Kết quả confidence scoring.
    /// </summary>
    public record ConfidenceScore(
        /// <summary>Overall confidence [0, 1]</summary>
        float Overall,

        /// <summary>Confidence từ retrieval (RAG scores)</summary>
        float RetrievalConfidence,

        /// <summary>Confidence từ reasoning quality</summary>
        float ReasoningConfidence,

        /// <summary>Giải thích về score</summary>
        string Justification
    )
    {
        /// <summary>
        /// Check if confidence is above threshold.
        /// </summary>
        public bool IsConfident(float threshold = 0.7f) => Overall >= threshold;

        /// <summary>
        /// Get confidence level label.
        /// </summary>
        public string GetLevel() => Overall switch
        {
            >= 0.9f => "Very High",
            >= 0.7f => "High",
            >= 0.5f => "Medium",
            >= 0.3f => "Low",
            _ => "Very Low"
        };
    }
}
