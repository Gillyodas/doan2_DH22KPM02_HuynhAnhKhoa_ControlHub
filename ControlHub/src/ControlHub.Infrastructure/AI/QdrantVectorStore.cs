using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using ControlHub.Application.Common.Interfaces.AI;
using Microsoft.Extensions.Logging;

namespace ControlHub.Infrastructure.AI
{
    public class QdrantVectorStore : IVectorDatabase
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<QdrantVectorStore> _logger;
        private const string QdrantUrl = "http://localhost:6333";

        public QdrantVectorStore(HttpClient httpClient, ILogger<QdrantVectorStore> logger)
        {
            _httpClient = httpClient;
            _logger = logger;
            _httpClient.BaseAddress = new Uri(QdrantUrl);
        }

        public async Task UpsertAsync(string collectionName, string id, float[] vector, Dictionary<string, object> payload)
        {
            // 1. Đảm bảo Collection tồn tại (nếu chưa có thì tạo)
            await EnsureCollectionExistsAsync(collectionName);

            // 2. Chuẩn bị request Upsert
            var point = new
            {
                points = new[]
                {
                    new
                    {
                        id = Guid.NewGuid().ToString(), // Qdrant thường cần UUID, hoặc số int. Ở đây ta random ID cho point, còn ID nghiệp vụ lưu trong payload
                        vector = vector,
                        payload = new Dictionary<string, object>(payload) { { "BusinessId", id } }
                    }
                }
            };

            var content = new StringContent(JsonSerializer.Serialize(point), Encoding.UTF8, "application/json");
            var response = await _httpClient.PutAsync($"/collections/{collectionName}/points?wait=true", content);

            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync();
                _logger.LogError("Failed to upsert to Qdrant: {Error}", error);
            }
        }

        public async Task<List<SearchResult>> SearchAsync(string collectionName, float[] vector, int limit = 3)
        {
            var searchRequest = new
            {
                vector = vector,
                limit = limit,
                with_payload = true
            };

            var content = new StringContent(JsonSerializer.Serialize(searchRequest), Encoding.UTF8, "application/json");
            var response = await _httpClient.PostAsync($"/collections/{collectionName}/points/search", content);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Search failed or collection not found.");
                return new List<SearchResult>();
            }

            var responseString = await response.Content.ReadAsStringAsync();
            var qdrantResult = JsonSerializer.Deserialize<QdrantSearchResponse>(responseString);

            var results = new List<SearchResult>();
            if (qdrantResult?.Result != null)
            {
                foreach (var item in qdrantResult.Result)
                {
                    results.Add(new SearchResult
                    {
                        Id = item.Payload != null && item.Payload.TryGetValue("BusinessId", out var idElement)
                             ? idElement.ToString()
                             : item.Id.ToString(), // Fallback
                        Score = item.Score,
                        Payload = item.Payload ?? new Dictionary<string, object>()
                    });
                }
            }
            return results;
        }

        private async Task EnsureCollectionExistsAsync(string collectionName)
        {
            // Kiểm tra collection
            var response = await _httpClient.GetAsync($"/collections/{collectionName}");
            if (!response.IsSuccessStatusCode)
            {
                // Tạo mới nếu chưa có (vector size = 768 cho nomic-embed-text, hoặc 384 cho all-minilm)
                // LƯU Ý: Size vector phải khớp với model Embedding bạn dùng. 
                // Ở đây mình để mặc định 384 (all-minilm-l6-v2 - model nhẹ phổ biến).
                var createRequest = new
                {
                    vectors = new { size = 384, distance = "Cosine" }
                };
                var content = new StringContent(JsonSerializer.Serialize(createRequest), Encoding.UTF8, "application/json");
                await _httpClient.PutAsync($"/collections/{collectionName}", content);
            }
        }

        // DTOs hứng response từ Qdrant
        private class QdrantSearchResponse
        {
            [JsonPropertyName("result")]
            public List<QdrantPoint>? Result { get; set; }
        }

        private class QdrantPoint
        {
            [JsonPropertyName("id")]
            public object Id { get; set; }
            [JsonPropertyName("score")]
            public double Score { get; set; }
            [JsonPropertyName("payload")]
            public Dictionary<string, object>? Payload { get; set; }
        }
    }
}
