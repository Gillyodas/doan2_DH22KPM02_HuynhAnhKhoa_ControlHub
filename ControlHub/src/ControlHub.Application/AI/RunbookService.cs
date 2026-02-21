using ControlHub.Application.Common.Interfaces.AI;
using Microsoft.Extensions.Configuration;

namespace ControlHub.Application.AI
{
    public class RunbookService : IRunbookService
    {
        private readonly IVectorDatabase _vectorDb;
        private readonly IEmbeddingService _embeddingService;
        private readonly string _collectionName;

        public RunbookService(
            IVectorDatabase vectorDb,
            IEmbeddingService embeddingService,
            IConfiguration config)
        {
            _vectorDb = vectorDb;
            _embeddingService = embeddingService;
            _collectionName = config["AuditAI:RunbookCollectionName"] ?? "Runbooks";
        }

        public async Task IngestRunbooksAsync(IEnumerable<RunbookEntry> runbooks)
        {
            foreach (var rb in runbooks)
            {
                // Key search text: LogCode + Problem + Tags
                var textToEmbed = $"Pattern: {rb.LogCode}. Problem: {rb.Problem}. Tags: {string.Join(",", rb.Tags)}";

                var vector = await _embeddingService.GenerateEmbeddingAsync(textToEmbed);
                if (vector.Length == 0) continue;

                var payload = new Dictionary<string, object>
                {
                    { "LogCode", rb.LogCode },
                    { "Problem", rb.Problem },
                    { "Solution", rb.Solution },
                    { "Tags", rb.Tags }
                };

                // ID is Hash of LogCode or Pattern? 
                // Using LogCode as ID might be restrictive if multiple runbooks map to same pattern.
                // Use Guid for ID in Qdrant, keep LogCode in payload.
                var id = System.Guid.NewGuid().ToString();

                await _vectorDb.UpsertAsync(_collectionName, id, vector, payload);
            }
        }

        public async Task<List<RunbookEntry>> FindRelatedRunbooksAsync(string logCodeOrPattern, int limit = 3)
        {
            var vector = await _embeddingService.GenerateEmbeddingAsync(logCodeOrPattern);
            if (vector.Length == 0) return new List<RunbookEntry>();

            var searchResults = await _vectorDb.SearchAsync(_collectionName, vector, limit);

            return searchResults.Select(r => new RunbookEntry(
                r.Payload.ContainsKey("LogCode") ? r.Payload["LogCode"].ToString() : "",
                r.Payload.ContainsKey("Problem") ? r.Payload["Problem"].ToString() : "",
                r.Payload.ContainsKey("Solution") ? r.Payload["Solution"].ToString() : "",
                r.Payload.ContainsKey("Tags") ? JsonSerializerHelper.DeserializeTags(r.Payload["Tags"]) : System.Array.Empty<string>()
            )).ToList();
        }

        // Helper to handle JArray/String conversion since Payload values are objects
        private static class JsonSerializerHelper
        {
            public static string[] DeserializeTags(object tagsObj)
            {
                try
                {
                    // This depends on how the VectorDB client deserializes JSON. 
                    // Assuming it comes back as JsonElement or JArray if serialized as object.
                    // For simplicity in this demo, safe casing.
                    if (tagsObj is string[] arr) return arr;
                    if (tagsObj is List<string> list) return list.ToArray();
                    return System.Array.Empty<string>();
                }
                catch
                {
                    return System.Array.Empty<string>();
                }
            }
        }
    }
}
