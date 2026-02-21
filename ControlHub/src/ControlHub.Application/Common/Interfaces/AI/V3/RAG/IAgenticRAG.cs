namespace ControlHub.Application.Common.Interfaces.AI.V3.RAG
{
    /// <summary>
    /// Agentic RAG tự động quyết định strategy (single-hop vs multi-hop) dựa trên query complexity.
    /// </summary>
    public interface IAgenticRAG
    {
        /// <summary>
        /// Retrieve documents với autonomous strategy selection.
        /// </summary>
        /// <param name="query">User query</param>
        /// <param name="options">RAG options</param>
        /// <param name="ct">Cancellation token</param>
        /// <returns>Agentic RAG result với strategy metadata</returns>
        Task<AgenticRAGResult> RetrieveAsync(
            string query,
            AgenticRAGOptions? options = null,
            CancellationToken ct = default
        );
    }

    /// <summary>
    /// Kết quả từ Agentic RAG.
    /// </summary>
    public record AgenticRAGResult(
        /// <summary>Retrieved documents (đã rerank)</summary>
        List<RankedDocument> Documents,

        /// <summary>Strategy đã được chọn</summary>
        RAGStrategy StrategyUsed,

        /// <summary>Metadata (hops, candidates, etc.)</summary>
        Dictionary<string, object> Metadata
    );

    /// <summary>
    /// RAG strategy được chọn bởi agent.
    /// </summary>
    public enum RAGStrategy
    {
        /// <summary>Single-hop retrieval + rerank</summary>
        SingleHop,

        /// <summary>Multi-hop iterative retrieval</summary>
        MultiHop,

        /// <summary>Hybrid approach (future)</summary>
        Hybrid
    }

    /// <summary>
    /// Options cho Agentic RAG.
    /// </summary>
    public record AgenticRAGOptions(
        /// <summary>Enable multi-hop cho complex queries (default: true)</summary>
        bool EnableMultiHop = true,

        /// <summary>Complexity threshold để trigger multi-hop (default: 0.5)</summary>
        float ComplexityThreshold = 0.5f,

        /// <summary>Max documents trả về (default: 10)</summary>
        int MaxDocuments = 10,

        /// <summary>CorrelationId to search in log files (null = skip log file search)</summary>
        string? CorrelationId = null
    );
}
