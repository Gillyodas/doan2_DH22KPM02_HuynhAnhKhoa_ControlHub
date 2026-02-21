using ControlHub.Application.Common.Interfaces.AI;
using ControlHub.Application.Common.Interfaces.AI.V3.RAG;
using Microsoft.Extensions.Logging;

namespace ControlHub.Application.AI.V3.RAG
{
    /// <summary>
    /// Multi-hop retriever thực hiện iterative retrieval cho complex queries.
    /// Mỗi hop sẽ expand query dựa trên context từ hop trước đó.
    /// </summary>
    public class MultiHopRetriever : IMultiHopRetriever
    {
        private readonly IVectorDatabase _vectorDb;
        private readonly IReranker _reranker;
        private readonly IEmbeddingService _embeddingService;
        private readonly ILogger<MultiHopRetriever> _logger;

        public MultiHopRetriever(
            IVectorDatabase vectorDb,
            IReranker reranker,
            IEmbeddingService embeddingService,
            ILogger<MultiHopRetriever> logger)
        {
            _vectorDb = vectorDb;
            _reranker = reranker;
            _embeddingService = embeddingService;
            _logger = logger;
        }

        public async Task<MultiHopResult> RetrieveAsync(
            string query,
            MultiHopOptions? options = null,
            CancellationToken ct = default)
        {
            options ??= new MultiHopOptions();
            var allDocuments = new List<RankedDocument>();
            var traces = new List<HopTrace>();
            var currentQuery = query;
            var seenContents = new HashSet<string>(); // Avoid duplicates

            _logger.LogInformation("Starting multi-hop retrieval for query: {Query}", query);

            for (int hop = 1; hop <= options.MaxHops; hop++)
            {
                _logger.LogDebug("Hop {HopNumber}: Query = {Query}", hop, currentQuery);

                // Step 1: Retrieve candidates from vector DB
                var embedding = await _embeddingService.GenerateEmbeddingAsync(currentQuery);
                var vectorResults = await _vectorDb.SearchAsync(
                    collectionName: "audit_logs",
                    vector: embedding,
                    limit: options.CandidatesPerHop
                );

                // Convert to RetrievedDocument
                var candidates = vectorResults
                    .Where(r => !seenContents.Contains(GetContentFromPayload(r.Payload))) // Filter duplicates
                    .Select(r => new RetrievedDocument(
                        GetContentFromPayload(r.Payload),
                        (float)r.Score,
                        new Dictionary<string, string> { ["source"] = "vector_db", ["id"] = r.Id }
                    ))
                    .ToList();

                if (candidates.Count == 0)
                {
                    _logger.LogWarning("Hop {HopNumber}: No new candidates found, stopping", hop);
                    break;
                }

                // Step 2: Rerank candidates
                var reranked = await _reranker.RerankAsync(currentQuery, candidates, options.TopKAfterRerank, ct);

                // Step 3: Add to results and mark as seen
                foreach (var doc in reranked)
                {
                    seenContents.Add(doc.Content);
                }
                allDocuments.AddRange(reranked);

                traces.Add(new HopTrace(
                    hop,
                    currentQuery,
                    candidates.Count,
                    reranked.Count
                ));

                // Step 4: Check if we have high confidence results
                var bestScore = reranked.FirstOrDefault()?.RelevanceScore ?? 0f;
                if (bestScore >= options.ConfidenceThreshold)
                {
                    _logger.LogInformation(
                        "Hop {HopNumber}: High confidence ({Score:F3}) reached, stopping early",
                        hop,
                        bestScore
                    );
                    break;
                }

                // Step 5: Expand query for next hop
                if (hop < options.MaxHops)
                {
                    currentQuery = ExpandQuery(query, reranked);
                    _logger.LogDebug("Hop {HopNumber}: Expanded query = {ExpandedQuery}", hop, currentQuery);
                }
            }

            _logger.LogInformation(
                "Multi-hop retrieval completed: {TotalHops} hops, {TotalDocs} documents",
                traces.Count,
                allDocuments.Count
            );

            return new MultiHopResult(allDocuments, traces, traces.Count);
        }

        /// <summary>
        /// Expand query dựa trên context từ top documents.
        /// Đơn giản: Thêm keywords từ top-1 document vào query.
        /// </summary>
        private string ExpandQuery(string originalQuery, List<RankedDocument> topDocs)
        {
            if (topDocs.Count == 0)
                return originalQuery;

            // Extract keywords from top document (simple heuristic)
            var topDoc = topDocs.First().Content;
            var keywords = ExtractKeywords(topDoc, maxKeywords: 3);

            // Combine original query with keywords
            var expandedQuery = $"{originalQuery} {string.Join(" ", keywords)}";
            return expandedQuery;
        }

        /// <summary>
        /// Extract keywords từ document (simple TF-based approach).
        /// </summary>
        private List<string> ExtractKeywords(string text, int maxKeywords = 3)
        {
            // Simple approach: Split by space, filter stopwords, take most frequent
            var stopwords = new HashSet<string> { "the", "a", "an", "and", "or", "but", "in", "on", "at", "to", "for" };

            var words = text
                .ToLower()
                .Split(new[] { ' ', ',', '.', '!', '?' }, StringSplitOptions.RemoveEmptyEntries)
                .Where(w => w.Length > 3 && !stopwords.Contains(w))
                .GroupBy(w => w)
                .OrderByDescending(g => g.Count())
                .Take(maxKeywords)
                .Select(g => g.Key)
                .ToList();

            return words;
        }

        /// <summary>
        /// Extract content from vector DB payload.
        /// </summary>
        private string GetContentFromPayload(Dictionary<string, object> payload)
        {
            if (payload.TryGetValue("Content", out var content))
                return content?.ToString() ?? string.Empty;
            if (payload.TryGetValue("Description", out var desc))
                return desc?.ToString() ?? string.Empty;
            return string.Empty;
        }
    }
}
