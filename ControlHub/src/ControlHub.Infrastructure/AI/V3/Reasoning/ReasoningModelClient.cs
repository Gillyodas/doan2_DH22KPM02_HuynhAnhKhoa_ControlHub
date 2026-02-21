using System.Text;
using System.Text.Json;
using ControlHub.Application.Common.Interfaces.AI.V3.Reasoning;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace ControlHub.Infrastructure.AI.V3.Reasoning
{
    /// <summary>
    /// Reasoning Model Client sử dụng Ollama LLM.
    /// Chain-of-Thought prompting để generate step-by-step solutions.
    /// </summary>
    public class ReasoningModelClient : IReasoningModel
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<ReasoningModelClient> _logger;
        private readonly string _ollamaUrl;
        private readonly string _modelName;

        public ReasoningModelClient(
            HttpClient httpClient,
            IConfiguration config,
            ILogger<ReasoningModelClient> logger)
        {
            _httpClient = httpClient;
            _logger = logger;
            _ollamaUrl = config["AI:OllamaUrl"] ?? "http://localhost:11434/api/generate";
            _modelName = config["AI:ModelName"] ?? "llama3";
        }

        public async Task<ReasoningResult> ReasonAsync(
            ReasoningContext context,
            ReasoningOptions? options = null,
            CancellationToken ct = default)
        {
            options ??= new ReasoningOptions();

            try
            {
                // Build prompt với Chain-of-Thought
                var prompt = BuildPrompt(context, options);

                _logger.LogInformation("Reasoning for query: {Query}", context.Query);

                // Call Ollama
                var response = await CallOllamaAsync(prompt, options, ct);

                // Parse response
                var result = ParseResponse(response, context);

                _logger.LogInformation(
                    "Reasoning completed: {StepCount} steps, confidence {Confidence:F2}",
                    result.Steps.Count,
                    result.Confidence
                );

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Reasoning failed for query: {Query}", context.Query);
                return new ReasoningResult(
                    Solution: "Unable to generate solution due to an error.",
                    Explanation: ex.Message,
                    Steps: new List<string>(),
                    Confidence: 0f,
                    RawResponse: null
                );
            }
        }

        private string BuildPrompt(ReasoningContext context, ReasoningOptions options)
        {
            var sb = new StringBuilder();

            // System context
            sb.AppendLine("You are an expert IT auditor and troubleshooter.");
            sb.AppendLine("Analyze the following information and provide a solution.");
            sb.AppendLine();

            // User query
            sb.AppendLine("## User Query:");
            sb.AppendLine(context.Query);
            sb.AppendLine();

            // Classification (if available)
            if (context.Classification != null)
            {
                sb.AppendLine("## Log Classification:");
                sb.AppendLine($"- Category: {context.Classification.Category}");
                sb.AppendLine($"- SubCategory: {context.Classification.SubCategory}");
                sb.AppendLine($"- Confidence: {context.Classification.Confidence:F2}");
                sb.AppendLine();
            }

            // Retrieved documents
            if (context.RetrievedDocs.Count > 0)
            {
                sb.AppendLine("## Related Logs/Evidence:");
                var docsToInclude = context.RetrievedDocs.Take(options.MaxContextDocs);
                int i = 1;
                foreach (var doc in docsToInclude)
                {
                    sb.AppendLine($"[{i}] (Score: {doc.RelevanceScore:F2})");
                    sb.AppendLine($"    {doc.Content}");
                    i++;
                }
                sb.AppendLine();
            }

            // Instructions with Chain-of-Thought
            if (options.EnableCoT)
            {
                sb.AppendLine("## Instructions:");
                sb.AppendLine("Think step by step and provide your response in the following JSON format. ");
                sb.AppendLine("CRITICAL: Ensure all internal quotes in 'solution' and 'explanation' are escaped with a backslash (\\\").");
                sb.AppendLine("IMPORTANT: Your entire response must be a single valid JSON object. Do not include any conversational text before or after the JSON.");
                sb.AppendLine("```json");
                sb.AppendLine("{");
                sb.AppendLine("  \"solution\": \"Brief solution summary (escape quotes such as \\\"like this\\\")\",");
                sb.AppendLine("  \"explanation\": \"Detailed explanation of the problem\",");
                sb.AppendLine("  \"steps\": [\"Step 1\", \"Step 2\", \"Step 3\"],");
                sb.AppendLine("  \"confidence\": 0.85");
                sb.AppendLine("}");
                sb.AppendLine("```");
            }
            else
            {
                sb.AppendLine("## Instructions:");
                sb.AppendLine("Provide a brief solution to the problem.");
            }

            // Background guidelines — placed LAST and explicitly marked as DO NOT ECHO
            sb.AppendLine();
            sb.AppendLine("## Background Guidelines (DO NOT include these as plan steps or findings):");
            sb.AppendLine("These are general analysis rules for YOUR internal use only. Do NOT repeat them in your output.");
            sb.AppendLine("- PRIORITIZE WARNING and ERROR level log entries — these contain the actual errors.");
            sb.AppendLine("- INFO level logs are context only. Do NOT treat INFO-level messages as errors.");
            sb.AppendLine("- 'CORS policy execution failed' logs are INFRASTRUCTURE NOISE. Ignore unless they cause a 403.");
            sb.AppendLine("- Look for domain-specific error codes (e.g., 'InvalidFormat', 'DomainError', 'Exception') as the TRUE root cause.");
            sb.AppendLine("- Check the HTTP status code in 'Request finished' to determine the actual outcome.");

            return sb.ToString();
        }

        private async Task<string> CallOllamaAsync(
            string prompt,
            ReasoningOptions options,
            CancellationToken ct)
        {
            var requestBody = new
            {
                model = _modelName,
                prompt = prompt,
                stream = false,
                options = new
                {
                    temperature = options.Temperature,
                    num_predict = options.MaxTokens
                }
            };

            var jsonRequest = JsonSerializer.Serialize(requestBody);
            var content = new StringContent(jsonRequest, Encoding.UTF8, "application/json");

            // Build a linked CancellationTokenSource with a hard timeout (5 minutes)
            using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
            cts.CancelAfter(TimeSpan.FromMinutes(5));

            try
            {
                // Use SendAsync with ResponseHeadersRead for efficiency and better control
                var request = new HttpRequestMessage(HttpMethod.Post, _ollamaUrl) { Content = content };
                var response = await _httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cts.Token);

                response.EnsureSuccessStatusCode();

                // Read entire response into string
                var responseJson = await response.Content.ReadAsStringAsync(cts.Token);

                using var jsonDoc = JsonDocument.Parse(responseJson);
                if (jsonDoc.RootElement.TryGetProperty("response", out var respProp))
                {
                    return respProp.GetString() ?? string.Empty;
                }

                _logger.LogWarning("LLM response did not contain 'response' property. Raw: {Raw}",
                    responseJson.Length > 200 ? responseJson.Substring(0, 200) : responseJson);

                return string.Empty;
            }
            catch (OperationCanceledException) when (!ct.IsCancellationRequested)
            {
                _logger.LogError("Ollama request timed out after 5 minutes at {Url}", _ollamaUrl);
                throw new TimeoutException($"The reasoning request to Ollama ({_modelName}) timed out after 5 minutes.");
            }
        }

        private ReasoningResult ParseResponse(string rawResponse, ReasoningContext context)
        {
            if (string.IsNullOrWhiteSpace(rawResponse))
                return GetErrorResult("Empty response from LLM", rawResponse);

            try
            {
                // 1. Pre-sanitize: Remove common markdown and escaping artifacts
                var cleaned = SanitizeJson(rawResponse);

                // 2. Find JSON blocks
                var jsonStart = cleaned.IndexOf('{');
                var jsonEnd = cleaned.LastIndexOf('}');

                if (jsonStart >= 0 && jsonEnd > jsonStart)
                {
                    var jsonStr = cleaned.Substring(jsonStart, jsonEnd - jsonStart + 1);
                    try
                    {
                        return ParseJson(jsonStr, rawResponse);
                    }
                    catch (JsonException)
                    {
                        _logger.LogWarning("Standard JSON parse failed, attempting aggressive cleaning...");

                        // 3. Aggressive cleaning: Remove all escaped double quotes and literal backslashes
                        var aggressive = jsonStr.Replace("\\\"", "\"").Replace("\\\\", "\\");
                        try
                        {
                            return ParseJson(aggressive, rawResponse);
                        }
                        catch (JsonException ex)
                        {
                            _logger.LogWarning(ex, "Aggressive cleaning failed. Attempting Regex extraction fallback...");

                            // 4. Regex fallback extraction
                            return ExtractWithRegex(jsonStr, rawResponse);
                        }
                    }
                }
                else
                {
                    // No JSON brackets found, try regex on whole response
                    return ExtractWithRegex(cleaned, rawResponse);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Unexpected error during JSON parsing. Raw: {Raw}", rawResponse);
            }

            // Fallback: use raw response as solution if it looks somewhat meaningful
            return new ReasoningResult(
                Solution: rawResponse.Length > 200 ? "ID_" + rawResponse.GetHashCode().ToString("X") + ": " + rawResponse.Substring(0, 200) + "..." : rawResponse,
                Explanation: rawResponse,
                Steps: new List<string>(),
                Confidence: 0.3f,
                RawResponse: rawResponse
            );
        }

        private ReasoningResult GetErrorResult(string message, string? raw)
        {
            return new ReasoningResult(
                Solution: "Analysis failed due to response format issues.",
                Explanation: message,
                Steps: new List<string>(),
                Confidence: 0f,
                RawResponse: raw
            );
        }

        private ReasoningResult ParseJson(string jsonStr, string rawResponse)
        {
            var options = new JsonDocumentOptions { AllowTrailingCommas = true };
            using var doc = JsonDocument.Parse(jsonStr, options);
            var root = doc.RootElement;

            var solution = GetStringProperty(root, "solution");
            var explanation = GetStringProperty(root, "explanation");
            var confidence = root.TryGetProperty("confidence", out var c) ? (float)c.GetDouble() : 0.5f;

            var steps = new List<string>();
            if (root.TryGetProperty("steps", out var stepsArr) && stepsArr.ValueKind == JsonValueKind.Array)
            {
                foreach (var stepElement in stepsArr.EnumerateArray())
                {
                    if (stepElement.ValueKind == JsonValueKind.String)
                    {
                        var stepText = stepElement.GetString();
                        if (!string.IsNullOrEmpty(stepText)) steps.Add(stepText);
                    }
                    else if (stepElement.ValueKind == JsonValueKind.Object)
                    {
                        // Handle hierarchical step objects like { "step": "...", "description": "..." }
                        var title = GetStringProperty(stepElement, "step") ?? GetStringProperty(stepElement, "name");
                        var desc = GetStringProperty(stepElement, "description") ?? GetStringProperty(stepElement, "content");

                        var combined = !string.IsNullOrEmpty(title) && !string.IsNullOrEmpty(desc)
                            ? $"{title}: {desc}"
                            : title ?? desc;

                        if (!string.IsNullOrEmpty(combined)) steps.Add(combined);
                    }
                }
            }

            return new ReasoningResult(solution, explanation, steps, confidence, rawResponse);
        }

        private string GetStringProperty(JsonElement element, string propName)
        {
            return element.TryGetProperty(propName, out var p) ? p.GetString() ?? "" : "";
        }

        /// <summary>
        /// Sanitizes LLM response to ensure it can be parsed as JSON.
        /// </summary>
        private string SanitizeJson(string input)
        {
            if (string.IsNullOrEmpty(input)) return input;

            // Remove Markdown code block markers
            var result = System.Text.RegularExpressions.Regex.Replace(input, @"```(json)?", "");

            // If the whole thing is wrapped in quotes (Ollama sometimes does this with double-escaping)
            if (result.Trim().StartsWith("\"") && result.Trim().EndsWith("\""))
            {
                result = result.Trim().Substring(1, result.Trim().Length - 2);
            }

            // Fix unescaped backslashes in paths NOT followed by valid escape sequence
            result = System.Text.RegularExpressions.Regex.Replace(result, @"(?<!\\)\\(?![""\\/bfnrtu])", @"\\");

            // Handle common LLM error: "key": "value" missing comma before next key
            result = System.Text.RegularExpressions.Regex.Replace(result, @"(""\s*:\s*""[^""]+"")\s+(""\w+""\s*:)", "$1, $2");

            return result;
        }

        /// <summary>
        /// Fallback method to extract fields using Regex if JSON parsing fails.
        /// Handles both simple string arrays and nested object arrays for "steps".
        /// </summary>
        private ReasoningResult ExtractWithRegex(string input, string rawResponse)
        {
            _logger.LogInformation("Regex fallback: extracting structured fields from malformed JSON");

            var solutionMatch = System.Text.RegularExpressions.Regex.Match(input, @"""solution""\s*:\s*""([^""]+)""");
            var explanationMatch = System.Text.RegularExpressions.Regex.Match(input, @"""explanation""\s*:\s*""([^""]+)""");
            var stepsMatch = System.Text.RegularExpressions.Regex.Match(input, @"""steps""\s*:\s*\[(.*?)\]", System.Text.RegularExpressions.RegexOptions.Singleline);

            var solution = solutionMatch.Success ? solutionMatch.Groups[1].Value : "Partial Diagnosis";
            var explanation = explanationMatch.Success ? explanationMatch.Groups[1].Value : "Refer to raw response";

            var steps = new List<string>();
            if (stepsMatch.Success)
            {
                var stepsContent = stepsMatch.Groups[1].Value;

                // Strategy 1: Try to match nested objects {"step": "...", "description": "..."}
                var nestedPattern = @"\{\s*""(?:step|name)""\s*:\s*""([^""]+)""\s*,\s*""(?:description|content)""\s*:\s*""([^""]+)""\s*\}";
                var nestedMatches = System.Text.RegularExpressions.Regex.Matches(stepsContent, nestedPattern, System.Text.RegularExpressions.RegexOptions.Singleline);

                if (nestedMatches.Count > 0)
                {
                    foreach (System.Text.RegularExpressions.Match m in nestedMatches)
                    {
                        var stepName = m.Groups[1].Value.Trim();
                        var stepDesc = m.Groups[2].Value.Trim();
                        steps.Add($"{stepName}: {stepDesc}");
                    }
                    _logger.LogInformation("Regex fallback: extracted {Count} steps from nested objects", steps.Count);
                }
                else
                {
                    // Strategy 2: Simple string array — but filter out short garbage like "step", "description"
                    var simpleMatches = System.Text.RegularExpressions.Regex.Matches(stepsContent, @"""([^""]+)""");
                    foreach (System.Text.RegularExpressions.Match m in simpleMatches)
                    {
                        var value = m.Groups[1].Value.Trim();
                        // Filter out JSON key names and very short strings
                        if (value.Length > 5 &&
                            !value.Equals("step", StringComparison.OrdinalIgnoreCase) &&
                            !value.Equals("name", StringComparison.OrdinalIgnoreCase) &&
                            !value.Equals("description", StringComparison.OrdinalIgnoreCase) &&
                            !value.Equals("content", StringComparison.OrdinalIgnoreCase))
                        {
                            steps.Add(value);
                        }
                    }
                    _logger.LogInformation("Regex fallback: extracted {Count} steps from simple strings", steps.Count);
                }
            }

            if (steps.Count == 0 && rawResponse.Contains("Root Cause Synthesis", StringComparison.OrdinalIgnoreCase))
            {
                steps.Add("Root Cause Synthesis and Recommendations");
            }

            return new ReasoningResult(
                Solution: solution,
                Explanation: explanation,
                Steps: steps,
                Confidence: 0.4f,
                RawResponse: rawResponse
            );
        }
    }
}
