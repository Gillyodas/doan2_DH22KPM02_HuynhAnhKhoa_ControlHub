namespace ControlHub.Application.Common.Interfaces.AI.V3.RAG
{
    /// <summary>
    /// Reranker sử dụng Cross-Encoder để tính relevance score chính xác hơn.
    /// Khác với bi-encoder (embedding similarity), cross-encoder xem xét query + document cùng lúc.
    /// </summary>
    public interface IReranker
    {
        /// <summary>
        /// Rerank danh sách candidates dựa trên query.
        /// </summary>
        /// <param name="query">User query</param>
        /// <param name="candidates">Danh sách documents từ initial retrieval</param>
        /// <param name="topK">Số lượng documents trả về sau khi rerank</param>
        /// <param name="ct">Cancellation token</param>
        /// <returns>Top-K documents được sắp xếp theo relevance score</returns>
        Task<List<RankedDocument>> RerankAsync(
            string query,
            List<RetrievedDocument> candidates,
            int topK = 5,
            CancellationToken ct = default
        );

        /// <summary>
        /// Tính relevance score cho một cặp query-document.
        /// </summary>
        /// <param name="query">User query</param>
        /// <param name="document">Document content</param>
        /// <param name="ct">Cancellation token</param>
        /// <returns>Relevance score (0.0 - 1.0)</returns>
        Task<float> ScoreAsync(
            string query,
            string document,
            CancellationToken ct = default
        );
    }

    /// <summary>
    /// Document sau khi được rerank với relevance score chính xác.
    /// </summary>
    public record RankedDocument(
        /// <summary>Document content</summary>
        string Content,

        /// <summary>Relevance score từ cross-encoder (0.0 - 1.0)</summary>
        float RelevanceScore,

        /// <summary>Metadata (source, timestamp, etc.)</summary>
        Dictionary<string, string> Metadata
    );

    /// <summary>
    /// Document từ initial retrieval (vector search).
    /// </summary>
    public record RetrievedDocument(
        /// <summary>Document content</summary>
        string Content,

        /// <summary>Initial score từ vector similarity</summary>
        float InitialScore,

        /// <summary>Metadata (source, timestamp, etc.)</summary>
        Dictionary<string, string> Metadata
    );
}
