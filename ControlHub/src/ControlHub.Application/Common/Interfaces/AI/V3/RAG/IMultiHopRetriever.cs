namespace ControlHub.Application.Common.Interfaces.AI.V3.RAG
{
    /// <summary>
    /// Multi-hop retriever thực hiện iterative retrieval cho complex queries.
    /// Ví dụ: "Why did user login fail after password reset?"
    /// - Hop 1: Retrieve về "password reset"
    /// - Hop 2: Expand query với context từ Hop 1, retrieve về "login failure"
    /// - Hop 3: Kết hợp để tìm root cause
    /// </summary>
    public interface IMultiHopRetriever
    {
        /// <summary>
        /// Thực hiện multi-hop retrieval.
        /// </summary>
        /// <param name="query">User query</param>
        /// <param name="options">Retrieval options (max hops, candidates per hop, etc.)</param>
        /// <param name="ct">Cancellation token</param>
        /// <returns>Multi-hop result với documents và traces</returns>
        Task<MultiHopResult> RetrieveAsync(
            string query,
            MultiHopOptions? options = null,
            CancellationToken ct = default
        );
    }

    /// <summary>
    /// Kết quả multi-hop retrieval.
    /// </summary>
    public record MultiHopResult(
        /// <summary>Tất cả documents từ các hops (đã rerank)</summary>
        List<RankedDocument> Documents,

        /// <summary>Trace của từng hop (để debug/observability)</summary>
        List<HopTrace> Traces,

        /// <summary>Tổng số hops đã thực hiện</summary>
        int TotalHops
    );

    /// <summary>
    /// Trace của một hop trong multi-hop retrieval.
    /// </summary>
    public record HopTrace(
        /// <summary>Hop number (1-indexed)</summary>
        int HopNumber,

        /// <summary>Query đã được expand cho hop này</summary>
        string ExpandedQuery,

        /// <summary>Số candidates retrieved</summary>
        int CandidatesRetrieved,

        /// <summary>Số candidates sau khi rerank</summary>
        int CandidatesAfterRerank
    );

    /// <summary>
    /// Options cho multi-hop retrieval.
    /// </summary>
    public record MultiHopOptions(
        /// <summary>Số hops tối đa (default: 3)</summary>
        int MaxHops = 3,

        /// <summary>Số candidates retrieve mỗi hop (default: 20)</summary>
        int CandidatesPerHop = 20,

        /// <summary>Top-K sau khi rerank mỗi hop (default: 5)</summary>
        int TopKAfterRerank = 5,

        /// <summary>Confidence threshold để dừng sớm (default: 0.7)</summary>
        float ConfidenceThreshold = 0.7f
    );
}
