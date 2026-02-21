using ControlHub.Application.Common.Interfaces.AI.V3.Parsing;
using ControlHub.Application.Common.Interfaces.AI.V3.RAG;

namespace ControlHub.Application.Common.Interfaces.AI.V3.Reasoning
{
    /// <summary>
    /// Reasoning Model interface - Sinh solution từ RAG results.
    /// Dùng LLM (Ollama) với Chain-of-Thought prompting.
    /// </summary>
    public interface IReasoningModel
    {
        /// <summary>
        /// Generate solution dựa trên context (query + retrieved docs).
        /// </summary>
        Task<ReasoningResult> ReasonAsync(
            ReasoningContext context,
            ReasoningOptions? options = null,
            CancellationToken ct = default
        );
    }

    /// <summary>
    /// Context cho reasoning - chứa tất cả thông tin cần thiết.
    /// </summary>
    public record ReasoningContext(
        /// <summary>Original user query</summary>
        string Query,

        /// <summary>Retrieved documents từ RAG</summary>
        List<RankedDocument> RetrievedDocs,

        /// <summary>Log classification từ Parsing (optional)</summary>
        LogClassification? Classification = null
    );

    /// <summary>
    /// Kết quả reasoning.
    /// </summary>
    public record ReasoningResult(
        /// <summary>Solution chính được đề xuất</summary>
        string Solution,

        /// <summary>Giải thích chi tiết</summary>
        string Explanation,

        /// <summary>Các bước thực hiện (step-by-step)</summary>
        List<string> Steps,

        /// <summary>Confidence score [0, 1]</summary>
        float Confidence,

        /// <summary>Raw LLM response (for debugging)</summary>
        string? RawResponse = null
    );

    /// <summary>
    /// Options cho Reasoning.
    /// </summary>
    public record ReasoningOptions(
        /// <summary>Max tokens cho response (default: 1000)</summary>
        int MaxTokens = 1000,

        /// <summary>Temperature cho LLM (default: 0.3 - more deterministic)</summary>
        float Temperature = 0.3f,

        /// <summary>Enable Chain-of-Thought prompting (default: true)</summary>
        bool EnableCoT = true,

        /// <summary>Max documents to include in context (default: 5)</summary>
        int MaxContextDocs = 5
    );
}
