using ControlHub.Application.AuditAI.Interfaces;
using ControlHub.Application.AuditAI.Interfaces.V3.RAG;
using ControlHub.Application.AuditAI.Logging.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace ControlHub.Infrastructure.AI.V3.RAG
{
    public class AgenticRAGService : IAgenticRAG
    {
        private readonly IVectorDatabase _vectorDb;
        private readonly IReranker _reranker;
        private readonly IMultiHopRetriever _multiHopRetriever;
        private readonly IEmbeddingService _embeddingService;
        private readonly ILogReaderService _logReader;
        private readonly ILogEvidenceProcessor _evidenceProcessor;
        private readonly IConfiguration _config;
        private readonly ILogger<AgenticRAGService> _logger;

        // Cache per-investigation (Scoped lifetime)
        private List<RetrievedDocument>? _cachedLogDocs;
        private string? _cachedCorrelationId;
        private EvidenceSummary? _cachedEvidenceSummary;

        public AgenticRAGService(
            IVectorDatabase vectorDb,
            IReranker reranker,
            IMultiHopRetriever multiHopRetriever,
            IEmbeddingService embeddingService,
            ILogReaderService logReader,
            ILogEvidenceProcessor evidenceProcessor,
            IConfiguration config,
            ILogger<AgenticRAGService> logger)
        {
            _vectorDb = vectorDb;
            _reranker = reranker;
            _multiHopRetriever = multiHopRetriever;
            _embeddingService = embeddingService;
            _logReader = logReader;
            _evidenceProcessor = evidenceProcessor;
            _config = config;
            _logger = logger;
        }

        public async Task<AgenticRAGResult> RetrieveAsync(
            string query,
            AgenticRAGOptions? options = null,
            CancellationToken ct = default)
        {
            options ??= new AgenticRAGOptions();

            var complexity = AnalyzeQueryComplexity(query);
            _logger.LogInformation(
                "Query complexity: {Complexity:F3}, CorrelationId: {Id}",
                complexity, options.CorrelationId ?? "null");

            if (!string.IsNullOrEmpty(options.CorrelationId))
            {
                _logger.LogInformation("CorrelationId provided, forcing SingleHop strategy");
                return await ExecuteSingleHopStrategy(query, options, ct);
            }

            return complexity >= options.ComplexityThreshold && options.EnableMultiHop
                ? await ExecuteMultiHopStrategy(query, options, ct)
                : await ExecuteSingleHopStrategy(query, options, ct);
        }

        private async Task<AgenticRAGResult> ExecuteSingleHopStrategy(
            string query, AgenticRAGOptions options, CancellationToken ct)
        {
            _logger.LogInformation("Using single-hop strategy, CorrelationId: {Id}", options.CorrelationId ?? "null");
            var candidates = new List<RetrievedDocument>();

            if (!string.IsNullOrEmpty(options.CorrelationId))
            {
                if (_cachedCorrelationId == options.CorrelationId && _cachedLogDocs != null)
                {
                    _logger.LogInformation("Using cached log entries ({Count} docs)", _cachedLogDocs.Count);
                    candidates.AddRange(_cachedLogDocs);
                }
                else
                {
                    var logEntries = await _logReader.GetLogsByCorrelationIdAsync(options.CorrelationId);
                    _logger.LogInformation("Found {Count} log entries", logEntries.Count);

                    var logDocs = logEntries.Select(entry =>
                    {
                        var metadata = new Dictionary<string, string>
                        {
                            ["source"] = "log_file",
                            ["timestamp"] = entry.Timestamp.ToString("o"),
                            ["level"] = entry.Level
                        };

                        // Pass structured fields so LogEvidenceProcessor can use them directly
                        if (entry.RequestId != null) metadata["requestId"] = entry.RequestId;
                        if (entry.SerilogTraceId != null) metadata["traceId"] = entry.SerilogTraceId;
                        if (entry.SourceContext != null) metadata["sourceContext"] = entry.SourceContext;
                        if (entry.SpanId != null) metadata["spanId"] = entry.SpanId;

                        // Extract structured fields from Properties (set by Serilog enrichers)
                        if (entry.Properties.TryGetValue("RequestPath", out var rp))
                            metadata["requestPath"] = rp.ToString()!;
                        if (entry.Properties.TryGetValue("StatusCode", out var sc))
                            metadata["statusCode"] = sc.ToString()!;
                        if (entry.Properties.TryGetValue("ElapsedMilliseconds", out var ms))
                            metadata["elapsedMs"] = ms.ToString()!;
                        if (entry.Properties.TryGetValue("Method", out var method))
                            metadata["method"] = method.ToString()!;

                        return new RetrievedDocument(
                            $"[{entry.Timestamp:yyyy-MM-dd HH:mm:ss}] [{entry.Level}] {entry.Message}",
                            0.95f,
                            metadata
                        );
                    }).ToList();

                    var evidence = _evidenceProcessor.ProcessLogs(logDocs);
                    _cachedEvidenceSummary = evidence;

                    candidates.Add(new RetrievedDocument(evidence.FormattedSummary, 1.0f,
                        new Dictionary<string, string> { ["source"] = "evidence_summary" }));
                    candidates.AddRange(evidence.PrioritizedLogs);

                    _cachedLogDocs = logDocs;
                    _cachedCorrelationId = options.CorrelationId;
                }
            }
            else
            {
                _logger.LogInformation("No correlationId, fetching recent Warning/Error logs for context");
                var recentLogs = await _logReader.GetRecentLogsAsync(100);
                var importantLogs = recentLogs.Where(l =>
                    l.Level is "Warning" or "Error" or "Fatal" or "Critical").ToList();

                candidates.AddRange(importantLogs.Select(entry => new RetrievedDocument(
                    $"[{entry.Timestamp:yyyy-MM-dd HH:mm:ss}] [{entry.Level}] {entry.Message}",
                    0.7f,
                    new Dictionary<string, string>
                    {
                        ["source"] = "recent_logs",
                        ["timestamp"] = entry.Timestamp.ToString("o"),
                        ["level"] = entry.Level
                    }
                )));
            }

            var collectionName = _config["AuditAI:RunbookCollectionName"] ?? "Runbooks";
            try
            {
                var embedding = await _embeddingService.GenerateEmbeddingAsync(query);
                var vectorResults = await _vectorDb.SearchAsync(collectionName, embedding, limit: 10);
                candidates.AddRange(vectorResults.Select(r => new RetrievedDocument(
                    GetContentFromPayload(r.Payload), (float)r.Score,
                    new Dictionary<string, string> { ["source"] = "vector_db", ["id"] = r.Id, ["is_runbook"] = "true" }
                )));
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Vector DB retrieval failed, continuing with logs only");
            }

            var reranked = candidates.Count > 0
                ? await _reranker.RerankAsync(query, candidates, options.MaxDocuments, ct)
                : new List<RankedDocument>();

            return new AgenticRAGResult(
                reranked,
                RAGStrategy.SingleHop,
                new Dictionary<string, object>
                {
                    ["candidates"] = candidates.Count,
                    ["log_entries"] = candidates.Count(c => c.Metadata.GetValueOrDefault("source") == "log_file"),
                    ["complexity"] = AnalyzeQueryComplexity(query),
                    ["evidence_metadata"] = _cachedEvidenceSummary?.ExtractedMetadata!,
                    ["evidence_summary"] = _cachedEvidenceSummary?.FormattedSummary ?? ""
                }
            );
        }

        private async Task<AgenticRAGResult> ExecuteMultiHopStrategy(
            string query, AgenticRAGOptions options, CancellationToken ct)
        {
            _logger.LogInformation("Using multi-hop strategy for query: {Query}", query);
            var multiHopOptions = new MultiHopOptions(MaxHops: 3, CandidatesPerHop: 20, TopKAfterRerank: 5, ConfidenceThreshold: 0.7f);
            var result = await _multiHopRetriever.RetrieveAsync(query, multiHopOptions, ct);

            return new AgenticRAGResult(
                result.Documents.Take(options.MaxDocuments).ToList(),
                RAGStrategy.MultiHop,
                new Dictionary<string, object>
                {
                    ["hops"] = result.TotalHops,
                    ["total_candidates"] = result.Documents.Count,
                    ["complexity"] = AnalyzeQueryComplexity(query)
                }
            );
        }

        private float AnalyzeQueryComplexity(string query)
        {
            var complexity = 0f;
            var wordCount = query.Split(' ', StringSplitOptions.RemoveEmptyEntries).Length;
            if (wordCount > 10) complexity += 0.3f;
            else if (wordCount > 5) complexity += 0.15f;

            var questionWords = new[] { "why", "how", "what if", "explain", "cause", "reason" };
            if (questionWords.Any(qw => query.ToLower().Contains(qw))) complexity += 0.4f;

            var conjunctions = new[] { " and ", " or ", " but ", " after ", " before " };
            if (conjunctions.Any(c => query.ToLower().Contains(c))) complexity += 0.3f;

            return Math.Min(complexity, 1.0f);
        }

        private string GetContentFromPayload(Dictionary<string, object> payload)
        {
            if (payload.TryGetValue("Content", out var content)) return content?.ToString() ?? string.Empty;
            if (payload.TryGetValue("Description", out var desc)) return desc?.ToString() ?? string.Empty;
            return string.Empty;
        }
    }
}
