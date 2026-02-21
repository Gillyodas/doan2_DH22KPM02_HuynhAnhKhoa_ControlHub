using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using ControlHub.Application.Common.Interfaces.AI;

namespace ControlHub.Infrastructure.AI
{
    public class OllamaEmbeddingService : IEmbeddingService
    {
        private readonly HttpClient _httpClient;
        private const string OllamaUrl = "http://localhost:11434/api/embeddings";
        private const string ModelName = "all-minilm"; // Model embedding nhẹ, nhớ pull về: ollama pull all-minilm

        public OllamaEmbeddingService(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        public async Task<float[]> GenerateEmbeddingAsync(string text)
        {
            var request = new
            {
                model = ModelName,
                prompt = text
            };

            var content = new StringContent(JsonSerializer.Serialize(request), Encoding.UTF8, "application/json");

            try
            {
                var response = await _httpClient.PostAsync(OllamaUrl, content);
                response.EnsureSuccessStatusCode();

                var responseString = await response.Content.ReadAsStringAsync();
                var result = JsonSerializer.Deserialize<OllamaEmbeddingResponse>(responseString);

                return result?.Embedding ?? Array.Empty<float>();
            }
            catch
            {
                // Fallback hoặc log error: Trả về mảng rỗng nếu lỗi để flow không chết
                return Array.Empty<float>();
            }
        }

        private class OllamaEmbeddingResponse
        {
            [JsonPropertyName("embedding")]
            public float[]? Embedding { get; set; }
        }
    }
}
