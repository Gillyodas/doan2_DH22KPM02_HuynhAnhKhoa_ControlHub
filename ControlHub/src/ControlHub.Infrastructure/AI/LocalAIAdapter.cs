using System.Text;
using System.Text.Json;
using ControlHub.Application.Common.Interfaces.AI;

namespace ControlHub.Infrastructure.AI
{
    public class LocalAIAdapter : IAIAnalysisService
    {
        private readonly HttpClient _httpClient;
        private readonly string _ollamaUrl;
        private readonly string _modelName;

        public LocalAIAdapter(HttpClient httpClient, Microsoft.Extensions.Configuration.IConfiguration configuration)
        {
            _httpClient = httpClient;
            _httpClient.Timeout = TimeSpan.FromMinutes(5);
            _ollamaUrl = configuration["AI:OllamaUrl"] ?? "http://localhost:11434/api/generate";
            _modelName = configuration["AI:ModelName"] ?? "llama3";
        }

        public async Task<string> AnalyzeLogsAsync(string prompt)
        {
            // Ollama API: POST /api/generate
            var requestBody = new
            {
                model = _modelName,
                prompt = prompt,
                stream = false,
                keep_alive = "10m"
            };

            var content = new StringContent(JsonSerializer.Serialize(requestBody), Encoding.UTF8, "application/json");

            try
            {
                var response = await _httpClient.PostAsync(_ollamaUrl, content);
                response.EnsureSuccessStatusCode();

                var responseString = await response.Content.ReadAsStringAsync();
                using var doc = JsonDocument.Parse(responseString);
                return doc.RootElement.GetProperty("response").GetString() ?? "No response.";
            }
            catch (Exception ex)
            {
                return $"AI Error: {ex.Message}";
            }
        }
    }
}
