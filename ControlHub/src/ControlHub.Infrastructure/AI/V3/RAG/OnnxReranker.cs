using ControlHub.Application.Common.Interfaces.AI.V3.RAG;
using FastBertTokenizer;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.ML.OnnxRuntime;
using Microsoft.ML.OnnxRuntime.Tensors;

namespace ControlHub.Infrastructure.AI.V3.RAG
{
    /// <summary>
    /// ONNX-based reranker sử dụng cross-encoder model (ms-marco-MiniLM-L-6-v2).
    /// Falls back to simple keyword-based scoring if model files are missing.
    /// </summary>
    public class OnnxReranker : IReranker, IDisposable
    {
        private readonly InferenceSession? _session;
        private readonly BertTokenizer? _tokenizer;
        private readonly ILogger<OnnxReranker> _logger;
        private readonly bool _modelLoaded;

        public OnnxReranker(IConfiguration config, ILogger<OnnxReranker> logger)
        {
            _logger = logger;

            // Resolve paths - read from correct config section
            var basePath = AppContext.BaseDirectory;
            var configModelPath = config["AuditAI:V3:RAG:RerankerModelPath"] ?? "Models/reranker.onnx";
            var configVocabPath = config["AuditAI:V3:RAG:RerankerVocabPath"] ?? "Models/reranker_vocab.txt";

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
                _logger.LogWarning("⚠️ Reranker ONNX model not found at {ModelPath}. Using keyword-based fallback.", modelPath);
                _modelLoaded = false;
                return;
            }

            if (!File.Exists(vocabPath))
            {
                _logger.LogWarning("⚠️ Reranker vocab not found at {VocabPath}. Using keyword-based fallback.", vocabPath);
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
                _logger.LogInformation("✅ OnnxReranker initialized with model: {ModelPath}", modelPath);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to load reranker model, using keyword-based fallback");
                _modelLoaded = false;
            }
        }

        public async Task<List<RankedDocument>> RerankAsync(
            string query,
            List<RetrievedDocument> candidates,
            int topK = 5,
            CancellationToken ct = default)
        {
            if (candidates == null || candidates.Count == 0)
            {
                _logger.LogWarning("No candidates provided for reranking");
                return new List<RankedDocument>();
            }

            var scoredDocs = new List<(RetrievedDocument doc, float score)>();

            foreach (var doc in candidates)
            {
                var score = await ScoreAsync(query, doc.Content, ct);
                scoredDocs.Add((doc, score));
            }

            var rankedDocs = scoredDocs
                .OrderByDescending(x => x.score)
                .Take(topK)
                .Select(x => new RankedDocument(
                    x.doc.Content,
                    x.score,
                    x.doc.Metadata
                ))
                .ToList();

            var bestDoc = rankedDocs.FirstOrDefault();
            _logger.LogInformation(
                "Reranked {CandidateCount} candidates to top {TopK} (Best Score: {BestScore:F4}, ModelLoaded: {ModelLoaded})",
                candidates.Count,
                topK,
                bestDoc?.RelevanceScore ?? 0f,
                _modelLoaded
            );

            return rankedDocs;
        }

        public async Task<float> ScoreAsync(
            string query,
            string document,
            CancellationToken ct = default)
        {
            if (!_modelLoaded || _tokenizer == null || _session == null)
            {
                // Keyword-based fallback scoring
                return KeywordBasedScore(query, document);
            }

            try
            {
                // Cross-encoder: concatenate query + document with [SEP] token
                var inputText = $"{query} [SEP] {document}";
                var encoded = _tokenizer.Encode(inputText, 512);

                var inputIds = encoded.InputIds.ToArray().Select(x => (long)x).ToArray();
                var attentionMask = encoded.AttentionMask.ToArray().Select(x => (long)x).ToArray();
                var tokenTypeIds = new long[inputIds.Length];

                // Set token_type_ids: 0 for query, 1 for document
                var sepIndex = Array.IndexOf(inputIds, 102L);
                if (sepIndex > 0)
                {
                    for (int i = sepIndex; i < tokenTypeIds.Length; i++)
                    {
                        tokenTypeIds[i] = 1;
                    }
                }

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

                // Apply sigmoid to normalize to [0, 1]
                var rawScore = output[0];
                var normalizedScore = 1.0f / (1.0f + MathF.Exp(-rawScore));

                return normalizedScore;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Reranking failed for query: {Query}", query);
                return KeywordBasedScore(query, document);
            }
        }

        /// <summary>
        /// Keyword-based fallback scoring when ONNX model is not available.
        /// </summary>
        private float KeywordBasedScore(string query, string document)
        {
            if (string.IsNullOrWhiteSpace(query) || string.IsNullOrWhiteSpace(document))
                return 0f;

            var queryWords = query.ToLowerInvariant()
                .Split(new[] { ' ', ',', '.', '!', '?' }, StringSplitOptions.RemoveEmptyEntries)
                .Where(w => w.Length > 2)
                .Distinct()
                .ToList();

            var docLower = document.ToLowerInvariant();
            var matchCount = queryWords.Count(word => docLower.Contains(word));

            if (queryWords.Count == 0) return 0.3f;

            // Normalize score: matched words / total query words
            var score = (float)matchCount / queryWords.Count;

            // Boost if document is shorter (more relevant)
            var lengthFactor = Math.Min(1.0f, 500f / document.Length);

            return Math.Min(1.0f, score * 0.7f + lengthFactor * 0.3f);
        }

        public void Dispose()
        {
            _session?.Dispose();
        }
    }
}
