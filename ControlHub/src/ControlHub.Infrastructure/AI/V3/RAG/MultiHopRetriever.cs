using ControlHub.Application.AuditAI.Interfaces;
using ControlHub.Application.AuditAI.Interfaces.V3.RAG;
using Microsoft.Extensions.Logging;

namespace ControlHub.Infrastructure.AI.V3.RAG
{
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
            var seenContents = new HashSet<string>();

            _logger.LogInformation("Starting multi-hop retrieval for query: {Query}", query);

            for (int hop = 1; hop <= options.MaxHops; hop++)
            {
                _logger.LogDebug("Hop {HopNumber}: Query = {Query}", hop, currentQuery);

                var embedding = await _embeddingService.GenerateEmbeddingAsync(currentQuery);
                var vectorResults = await _vectorDb.SearchAsync("audit_logs", embedding, limit: options.CandidatesPerHop);

                var candidates = vectorResults
                    .Where(r => !seenContents.Contains(GetContentFromPayload(r.Payload)))
                    .Select(r => new RetrievedDocument(
                        GetContentFromPayload(r.Payload), (float)r.Score,
                        new Dictionary<string, string> { ["source"] = "vector_db", ["id"] = r.Id }
                    )).ToList();

                if (candidates.Count == 0)
                {
                    _logger.LogWarning("Hop {HopNumber}: No new candidates found, stopping", hop);
                    break;
                }

                var reranked = await _reranker.RerankAsync(currentQuery, candidates, options.TopKAfterRerank, ct);
                foreach (var doc in reranked) seenContents.Add(doc.Content);
                allDocuments.AddRange(reranked);

                traces.Add(new HopTrace(hop, currentQuery, candidates.Count, reranked.Count));

                var bestScore = reranked.FirstOrDefault()?.RelevanceScore ?? 0f;
                if (bestScore >= options.ConfidenceThreshold)
                {
                    _logger.LogInformation("Hop {HopNumber}: High confidence ({Score:F3}) reached", hop, bestScore);
                    break;
                }

                if (hop < options.MaxHops)
                    currentQuery = ExpandQuery(query, reranked);
            }

            _logger.LogInformation("Multi-hop completed: {Hops} hops, {Docs} documents", traces.Count, allDocuments.Count);
            return new MultiHopResult(allDocuments, traces, traces.Count);
        }

        private string ExpandQuery(string originalQuery, List<RankedDocument> topDocs)
        {
            if (topDocs.Count == 0) return originalQuery;
            var keywords = ExtractKeywords(topDocs.First().Content, maxKeywords: 3);
            return $"{originalQuery} {string.Join(" ", keywords)}";
        }

        private List<string> ExtractKeywords(string text, int maxKeywords = 3)
        {
            var stopwords = new HashSet<string> { "the", "a", "an", "and", "or", "but", "in", "on", "at", "to", "for" };
            return text.ToLower()
                .Split(new[] { ' ', ',', '.', '!', '?' }, StringSplitOptions.RemoveEmptyEntries)
                .Where(w => w.Length > 3 && !stopwords.Contains(w))
                .GroupBy(w => w)
                .OrderByDescending(g => g.Count())
                .Take(maxKeywords)
                .Select(g => g.Key)
                .ToList();
        }

        private string GetContentFromPayload(Dictionary<string, object> payload)
        {
            if (payload.TryGetValue("Content", out var content)) return content?.ToString() ?? string.Empty;
            if (payload.TryGetValue("Description", out var desc)) return desc?.ToString() ?? string.Empty;
            return string.Empty;
        }
    }
}
