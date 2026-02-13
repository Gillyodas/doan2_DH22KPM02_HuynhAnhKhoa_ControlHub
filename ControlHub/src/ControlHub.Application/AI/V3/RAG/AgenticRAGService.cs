using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ControlHub.Application.Common.Interfaces.AI;
using ControlHub.Application.Common.Interfaces.AI.V3.RAG;
using ControlHub.Application.Common.Logging.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace ControlHub.Application.AI.V3.RAG
{
    /// <summary>
    /// Agentic RAG tự động quyết định strategy dựa trên query complexity.
    /// Simple queries → Single-hop + Rerank
    /// Complex queries → Multi-hop retrieval
    /// With correlationId → Read from log files first!
    /// </summary>
    public class AgenticRAGService : IAgenticRAG
    {
        private readonly IVectorDatabase _vectorDb;
        private readonly IReranker _reranker;
        private readonly IMultiHopRetriever _multiHopRetriever;
        private readonly IEmbeddingService _embeddingService;
        private readonly ILogReaderService _logReader;
        private readonly IConfiguration _config;
        private readonly ILogger<AgenticRAGService> _logger;
        
        // Cache per-investigation (Scoped lifetime)
        private List<RetrievedDocument>? _cachedLogDocs;
        private string? _cachedCorrelationId;

        public AgenticRAGService(
            IVectorDatabase vectorDb,
            IReranker reranker,
            IMultiHopRetriever multiHopRetriever,
            IEmbeddingService embeddingService,
            ILogReaderService logReader,
            IConfiguration config,
            ILogger<AgenticRAGService> logger)
        {
            _vectorDb = vectorDb;
            _reranker = reranker;
            _multiHopRetriever = multiHopRetriever;
            _embeddingService = embeddingService;
            _logReader = logReader;
            _config = config;
            _logger = logger;
        }

        public async Task<AgenticRAGResult> RetrieveAsync(
            string query,
            AgenticRAGOptions? options = null,
            CancellationToken ct = default)
        {
            options ??= new AgenticRAGOptions();

            // Step 1: Analyze query complexity
            var complexity = AnalyzeQueryComplexity(query);
            _logger.LogInformation(
                "Query complexity: {Complexity:F3} (threshold: {Threshold:F3}), CorrelationId: {Id}",
                complexity,
                options.ComplexityThreshold,
                options.CorrelationId ?? "null"
            );

            // Step 2: Choose strategy
            // FORCE SingleHop when correlationId is provided (to read from log files!)
            if (!string.IsNullOrEmpty(options.CorrelationId))
            {
                _logger.LogInformation("CorrelationId provided, forcing SingleHop strategy for log file reading");
                return await ExecuteSingleHopStrategy(query, options, ct);
            }

            if (complexity >= options.ComplexityThreshold && options.EnableMultiHop)
            {
                return await ExecuteMultiHopStrategy(query, options, ct);
            }
            else
            {
                return await ExecuteSingleHopStrategy(query, options, ct);
            }
        }

        /// <summary>
        /// Execute single-hop strategy: Retrieve + Rerank.
        /// If correlationId provided, read from log files first!
        /// </summary>
        private async Task<AgenticRAGResult> ExecuteSingleHopStrategy(
            string query,
            AgenticRAGOptions options,
            CancellationToken ct)
        {
            _logger.LogInformation("Using single-hop strategy for query: {Query}, CorrelationId: {Id}", 
                query, options.CorrelationId ?? "null");

            var candidates = new List<RetrievedDocument>();

            // Step 1: If correlationId provided, read from log files (with caching)
            if (!string.IsNullOrEmpty(options.CorrelationId))
            {
                if (_cachedCorrelationId == options.CorrelationId && _cachedLogDocs != null)
                {
                    _logger.LogInformation("Using cached log entries for correlationId: {Id} ({Count} docs)", 
                        options.CorrelationId, _cachedLogDocs.Count);
                    candidates.AddRange(_cachedLogDocs);
                }
                else
                {
                    _logger.LogInformation("Reading logs for correlationId: {Id}", options.CorrelationId);
                    
                    var logEntries = await _logReader.GetLogsByCorrelationIdAsync(options.CorrelationId);
                    
                    _logger.LogInformation("Found {Count} log entries for correlationId: {Id}", 
                        logEntries.Count, options.CorrelationId);

                    var logDocs = new List<RetrievedDocument>();
                    // Convert log entries to documents
                    foreach (var entry in logEntries)
                    {
                        var content = $"[{entry.Timestamp:yyyy-MM-dd HH:mm:ss}] [{entry.Level}] {entry.Message}";
                        logDocs.Add(new RetrievedDocument(
                            content,
                            0.95f, // High score for direct matches
                            new Dictionary<string, string> 
                            { 
                                ["source"] = "log_file",
                                ["timestamp"] = entry.Timestamp.ToString("o"),
                                ["level"] = entry.Level
                            }
                        ));
                    }
                    
                    _cachedLogDocs = logDocs;
                    _cachedCorrelationId = options.CorrelationId;
                    candidates.AddRange(logDocs);
                }
            }
            // Step 1b: If NO correlationId, read recent logs for general context
            else
            {
                _logger.LogInformation("No correlationId, fetching recent Warning/Error logs for context");
                var recentLogs = await _logReader.GetRecentLogsAsync(100);
                
                var importantLogs = recentLogs.Where(l => 
                    l.Level == "Warning" || l.Level == "Error" || l.Level == "Fatal" || l.Level == "Critical"
                ).ToList();

                _logger.LogInformation("Found {Count} important entries in recent logs", importantLogs.Count);

                foreach (var entry in importantLogs)
                {
                    var content = $"[{entry.Timestamp:yyyy-MM-dd HH:mm:ss}] [{entry.Level}] {entry.Message}";
                    candidates.Add(new RetrievedDocument(
                        content,
                        0.7f, // Medium score for context logs
                        new Dictionary<string, string> 
                        { 
                            ["source"] = "recent_logs",
                            ["timestamp"] = entry.Timestamp.ToString("o"),
                            ["level"] = entry.Level
                        }
                    ));
                }
            }

            // Step 2: ALWAYS search Vector DB for Knowledge/Runbooks (Hybrid RAG)
            var collectionName = _config["AuditAI:RunbookCollectionName"] ?? "Runbooks";
            _logger.LogInformation("Searching vector DB collection '{Collection}' for knowledge/runbooks: {Query}", collectionName, query);
            try
            {
                var embedding = await _embeddingService.GenerateEmbeddingAsync(query);
                var vectorResults = await _vectorDb.SearchAsync(
                    collectionName: collectionName,
                    vector: embedding,
                    limit: 10 // Get relevant runbooks/history
                );

                foreach (var r in vectorResults)
                {
                    candidates.Add(new RetrievedDocument(
                        GetContentFromPayload(r.Payload),
                        (float)r.Score,
                        new Dictionary<string, string> 
                        { 
                            ["source"] = "vector_db", 
                            ["id"] = r.Id,
                            ["is_runbook"] = "true" 
                        }
                    ));
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Vector DB knowledge retrieval failed, continuing with available logs");
            }

            // Step 3: Rerank (if we have candidates)
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
                    ["complexity"] = AnalyzeQueryComplexity(query)
                }
            );
        }

        /// <summary>
        /// Execute multi-hop strategy: Iterative retrieval with query expansion.
        /// </summary>
        private async Task<AgenticRAGResult> ExecuteMultiHopStrategy(
            string query,
            AgenticRAGOptions options,
            CancellationToken ct)
        {
            _logger.LogInformation("Using multi-hop strategy for query: {Query}", query);

            var multiHopOptions = new MultiHopOptions(
                MaxHops: 3,
                CandidatesPerHop: 20,
                TopKAfterRerank: 5,
                ConfidenceThreshold: 0.7f
            );

            var result = await _multiHopRetriever.RetrieveAsync(query, multiHopOptions, ct);

            // Limit to MaxDocuments
            var finalDocs = result.Documents.Take(options.MaxDocuments).ToList();

            return new AgenticRAGResult(
                finalDocs,
                RAGStrategy.MultiHop,
                new Dictionary<string, object>
                {
                    ["hops"] = result.TotalHops,
                    ["total_candidates"] = result.Documents.Count,
                    ["complexity"] = AnalyzeQueryComplexity(query)
                }
            );
        }

        /// <summary>
        /// Analyze query complexity using heuristics.
        /// Returns score 0.0 - 1.0 (higher = more complex).
        /// </summary>
        private float AnalyzeQueryComplexity(string query)
        {
            var complexity = 0f;

            // Heuristic 1: Word count (longer queries tend to be more complex)
            var wordCount = query.Split(' ', StringSplitOptions.RemoveEmptyEntries).Length;
            if (wordCount > 10) complexity += 0.3f;
            else if (wordCount > 5) complexity += 0.15f;

            // Heuristic 2: Question words (why, how, what if → complex)
            var questionWords = new[] { "why", "how", "what if", "explain", "cause", "reason" };
            if (questionWords.Any(qw => query.ToLower().Contains(qw)))
                complexity += 0.4f;

            // Heuristic 3: Conjunctions (and, or, but → multiple concepts)
            var conjunctions = new[] { " and ", " or ", " but ", " after ", " before " };
            if (conjunctions.Any(c => query.ToLower().Contains(c)))
                complexity += 0.3f;

            return Math.Min(complexity, 1.0f);
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
