using ControlHub.Application.Common.Interfaces.AI.V3.Parsing;
using FastBertTokenizer;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.ML.OnnxRuntime;
using Microsoft.ML.OnnxRuntime.Tensors;

namespace ControlHub.Infrastructure.AI.V3.ML
{
    /// <summary>
    /// ONNX-based semantic log classifier.
    /// Falls back to rule-based classification if model files are missing.
    /// </summary>
    public class OnnxLogClassifier : ISemanticLogClassifier, IDisposable
    {
        private readonly InferenceSession? _session;
        private readonly BertTokenizer? _tokenizer;
        private readonly ILogger<OnnxLogClassifier> _logger;
        private readonly string[] _labels = { "authentication", "authorization", "database", "network", "system", "general" };
        private readonly bool _modelLoaded;

        public OnnxLogClassifier(IConfiguration config, ILogger<OnnxLogClassifier> logger)
        {
            _logger = logger;

            // Resolve paths - read from correct config section
            var basePath = AppContext.BaseDirectory;
            var configModelPath = config["AuditAI:V3:Parsing:OnnxModelPath"] ?? "Models/log_classifier.onnx";
            var configVocabPath = config["AuditAI:V3:Parsing:VocabPath"] ?? "Models/vocab.txt";

            // If path is relative, make it absolute from base directory
            var modelPath = Path.IsPathRooted(configModelPath)
                ? configModelPath
                : Path.Combine(basePath, configModelPath);
            var vocabPath = Path.IsPathRooted(configVocabPath)
                ? configVocabPath
                : Path.Combine(basePath, configVocabPath);

            // Graceful fallback if model files don't exist
            if (!File.Exists(modelPath))
            {
                //TODO: Format log
                _logger.LogWarning("?? ONNX model not found at {ModelPath}. Using rule-based fallback.", modelPath);
                _modelLoaded = false;
                return;
            }

            if (!File.Exists(vocabPath))
            {
                _logger.LogWarning("?? Vocab file not found at {VocabPath}. Using rule-based fallback.", vocabPath);
                _modelLoaded = false;
                return;
            }

            try
            {
                _session = new InferenceSession(modelPath);
                _tokenizer = new BertTokenizer();

                using (var reader = new StreamReader(vocabPath))
                {
                    _tokenizer.LoadVocabulary(reader, convertInputToLowercase: true);
                }

                _modelLoaded = true;
                _logger.LogInformation("? ONNX log classifier loaded successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to load ONNX model, using rule-based fallback");
                _modelLoaded = false;
            }
        }

        public async Task<LogClassification> ClassifyAsync(string logLine, CancellationToken ct = default)
        {
            try
            {
                if (_modelLoaded && _session != null && _tokenizer != null)
                {
                    var (category, confidence) = await RunInferenceAsync(logLine, ct);
                    return new LogClassification(
                        Category: category,
                        SubCategory: "general",
                        Confidence: confidence,
                        ExtractedFields: new Dictionary<string, string>()
                    );
                }
                else
                {
                    // Rule-based fallback classification
                    return RuleBasedClassify(logLine);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Semantic classification failed for log line: {LogLine}", logLine);
                return new LogClassification("unknown", "none", 0.0f, new Dictionary<string, string>());
            }
        }

        public async Task<float> GetConfidenceAsync(string logLine, string expectedCategory, CancellationToken ct = default)
        {
            if (!_modelLoaded)
            {
                var fallback = RuleBasedClassify(logLine);
                return fallback.Category.Equals(expectedCategory, StringComparison.OrdinalIgnoreCase)
                    ? fallback.Confidence
                    : 0.0f;
            }

            var (category, confidence) = await RunInferenceAsync(logLine, ct);
            return category.Equals(expectedCategory, StringComparison.OrdinalIgnoreCase) ? confidence : 0.0f;
        }

        /// <summary>
        /// Rule-based fallback when ONNX model is not available.
        /// </summary>
        private LogClassification RuleBasedClassify(string logLine)
        {
            var lower = logLine.ToLowerInvariant();

            // Authentication keywords
            if (lower.Contains("login") || lower.Contains("logout") || lower.Contains("password") ||
                lower.Contains("signin") || lower.Contains("signout") || lower.Contains("authenticate"))
            {
                return new LogClassification("authentication", "login", 0.8f, new Dictionary<string, string>());
            }

            // Authorization keywords
            if (lower.Contains("permission") || lower.Contains("authorize") || lower.Contains("access denied") ||
                lower.Contains("forbidden") || lower.Contains("role") || lower.Contains("policy"))
            {
                return new LogClassification("authorization", "access", 0.8f, new Dictionary<string, string>());
            }

            // Database keywords
            if (lower.Contains("sql") || lower.Contains("database") || lower.Contains("query") ||
                lower.Contains("insert") || lower.Contains("update") || lower.Contains("delete") ||
                lower.Contains("connection") || lower.Contains("transaction"))
            {
                return new LogClassification("database", "query", 0.75f, new Dictionary<string, string>());
            }

            // Network keywords
            if (lower.Contains("http") || lower.Contains("request") || lower.Contains("response") ||
                lower.Contains("tcp") || lower.Contains("socket") || lower.Contains("timeout"))
            {
                return new LogClassification("network", "http", 0.75f, new Dictionary<string, string>());
            }

            // System keywords
            if (lower.Contains("exception") || lower.Contains("error") || lower.Contains("warning") ||
                lower.Contains("crash") || lower.Contains("memory") || lower.Contains("cpu"))
            {
                return new LogClassification("system", "error", 0.7f, new Dictionary<string, string>());
            }

            // Default
            return new LogClassification("general", "info", 0.5f, new Dictionary<string, string>());
        }

        private async Task<(string Category, float Confidence)> RunInferenceAsync(string logLine, CancellationToken ct)
        {
            if (_tokenizer == null || _session == null)
            {
                throw new InvalidOperationException("Model not loaded");
            }

            var encoded = _tokenizer.Encode(logLine, 128);

            var inputIds = encoded.InputIds.ToArray().Select(x => (long)x).ToArray();
            var attentionMask = encoded.AttentionMask.ToArray().Select(x => (long)x).ToArray();
            var tokenTypeIds = new long[inputIds.Length];

            var inputTensor = new DenseTensor<long>(inputIds, new[] { 1, inputIds.Length });
            var maskTensor = new DenseTensor<long>(attentionMask, new[] { 1, attentionMask.Length });
            var typeTensor = new DenseTensor<long>(tokenTypeIds, new[] { 1, tokenTypeIds.Length });

            var inputs = new List<NamedOnnxValue>
            {
                NamedOnnxValue.CreateFromTensor("input_ids", inputTensor),
                NamedOnnxValue.CreateFromTensor("attention_mask", maskTensor),
                NamedOnnxValue.CreateFromTensor("token_type_ids", typeTensor)
            };

            using var results = _session.Run(inputs);
            var output = results.First().AsEnumerable<float>().ToArray();

            var maxScore = output.Max();
            var maxIndex = Array.IndexOf(output, maxScore);
            var category = _labels[maxIndex];

            return (category, maxScore);
        }

        public void Dispose()
        {
            _session?.Dispose();
        }
    }
}
