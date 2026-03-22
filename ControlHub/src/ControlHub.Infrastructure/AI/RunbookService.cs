using ControlHub.Application.AuditAI.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace ControlHub.Infrastructure.AI
{
    public class RunbookService : IRunbookService
    {
        private readonly IEmbeddingService _embeddingService;
        private readonly IVectorDatabase _vectorDatabase;
        private readonly IConfiguration _configuration;
        private readonly ILogger<RunbookService> _logger;

        private string CollectionName =>
            _configuration["AuditAI:RunbookCollectionName"] ?? "Runbooks";

        public RunbookService(
            IEmbeddingService embeddingService,
            IVectorDatabase vectorDatabase,
            IConfiguration configuration,
            ILogger<RunbookService> logger)
        {
            _embeddingService = embeddingService;
            _vectorDatabase = vectorDatabase;
            _configuration = configuration;
            _logger = logger;
        }

        public async Task IngestRunbooksAsync(IEnumerable<RunbookEntry> runbooks)
        {
            var entries = runbooks.ToList();
            _logger.LogInformation("Ingesting {Count} runbooks into collection '{Collection}'",
                entries.Count, CollectionName);

            foreach (var runbook in entries)
            {
                // Combine all text for a rich embedding
                var textForEmbedding = $"{runbook.LogCode} {runbook.Problem} {runbook.Solution} {string.Join(" ", runbook.Tags)}";

                var embedding = await _embeddingService.GenerateEmbeddingAsync(textForEmbedding);

                var payload = new Dictionary<string, object>
                {
                    ["LogCode"] = runbook.LogCode,
                    ["Problem"] = runbook.Problem,
                    ["Solution"] = runbook.Solution,
                    ["Tags"] = runbook.Tags,
                    ["Content"] = $"[{runbook.LogCode}] {runbook.Problem}\nSolution: {runbook.Solution}"
                };

                await _vectorDatabase.UpsertAsync(CollectionName, runbook.LogCode, embedding, payload);

                _logger.LogDebug("Ingested runbook: {LogCode}", runbook.LogCode);
            }

            _logger.LogInformation("Successfully ingested {Count} runbooks", entries.Count);
        }

        public async Task<List<RunbookEntry>> FindRelatedRunbooksAsync(string logCodeOrPattern, int limit = 3)
        {
            _logger.LogInformation("Searching runbooks for: '{Pattern}' (limit: {Limit})",
                logCodeOrPattern, limit);

            var queryEmbedding = await _embeddingService.GenerateEmbeddingAsync(logCodeOrPattern);
            var results = await _vectorDatabase.SearchAsync(CollectionName, queryEmbedding, limit);

            var runbooks = results
                .Select(r =>
                {
                    var logCode = r.Payload.GetValueOrDefault("LogCode")?.ToString() ?? "";
                    var problem = r.Payload.GetValueOrDefault("Problem")?.ToString() ?? "";
                    var solution = r.Payload.GetValueOrDefault("Solution")?.ToString() ?? "";
                    var tags = r.Payload.GetValueOrDefault("Tags") switch
                    {
                        string[] arr => arr,
                        IEnumerable<object> list => list.Select(x => x.ToString() ?? "").ToArray(),
                        string s => new[] { s },
                        _ => Array.Empty<string>()
                    };

                    return new RunbookEntry(logCode, problem, solution, tags);
                })
                .ToList();

            _logger.LogInformation("Found {Count} related runbooks for '{Pattern}'",
                runbooks.Count, logCodeOrPattern);

            return runbooks;
        }
    }
}
