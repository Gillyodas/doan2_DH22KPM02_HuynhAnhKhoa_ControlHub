using ControlHub.Application.AI.V3.RAG;
using ControlHub.Application.Common.Interfaces.AI;
using ControlHub.Application.Common.Interfaces.AI.V3.RAG;
using ControlHub.Application.Common.Logging.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;

namespace ControlHub.Application.Tests.AI.V3.RAG
{
    public class AgenticRAGIntegrationTests
    {
        private readonly Mock<IVectorDatabase> _vectorDbMock = new();
        private readonly Mock<IReranker> _rerankerMock = new();
        private readonly Mock<IMultiHopRetriever> _multiHopRetrieverMock = new();
        private readonly Mock<IEmbeddingService> _embeddingServiceMock = new();
        private readonly Mock<ILogReaderService> _logReaderMock = new();
        private readonly Mock<ILogEvidenceProcessor> _evidenceProcessorMock = new();
        private readonly Mock<IConfiguration> _configMock = new();
        private readonly Mock<ILogger<AgenticRAGService>> _loggerMock = new();
        private readonly AgenticRAGService _service;

        public AgenticRAGIntegrationTests()
        {
            _service = new AgenticRAGService(
                _vectorDbMock.Object,
                _rerankerMock.Object,
                _multiHopRetrieverMock.Object,
                _embeddingServiceMock.Object,
                _logReaderMock.Object,
                _evidenceProcessorMock.Object,
                _configMock.Object,
                _loggerMock.Object
            );
        }

        [Fact]
        public async Task RetrieveAsync_WithSimpleQuery_ShouldUseSingleHop()
        {
            // Arrange
            var query = "authentication error";
            var embedding = new float[] { 0.1f, 0.2f, 0.3f };

            _embeddingServiceMock
                .Setup(x => x.GenerateEmbeddingAsync(query))
                .ReturnsAsync(embedding);

            _vectorDbMock
                .Setup(x => x.SearchAsync(
                    It.IsAny<string>(),
                    It.IsAny<float[]>(),
                    It.IsAny<int>()))
                .ReturnsAsync(new List<SearchResult>
                {
                    new() { Id = "1", Score = 0.9, Payload = new Dictionary<string, object> { ["Content"] = "Auth failed: invalid password" } }
                });

            _rerankerMock
                .Setup(x => x.RerankAsync(
                    query,
                    It.IsAny<List<RetrievedDocument>>(),
                    It.IsAny<int>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(new List<RankedDocument>
                {
                    new("Auth failed: invalid password", 0.95f, new Dictionary<string, string>())
                });

            // Act
            var result = await _service.RetrieveAsync(query);

            // Assert
            Assert.Equal(RAGStrategy.SingleHop, result.StrategyUsed);
            Assert.Single(result.Documents);
            _multiHopRetrieverMock.Verify(
                x => x.RetrieveAsync(It.IsAny<string>(), It.IsAny<MultiHopOptions>(), It.IsAny<CancellationToken>()),
                Times.Never
            );
        }

        [Fact]
        public async Task RetrieveAsync_WithComplexQuery_ShouldUseMultiHop()
        {
            // Arrange
            var query = "Why did user login fail after password reset and what was the root cause?";

            _multiHopRetrieverMock
                .Setup(x => x.RetrieveAsync(query, It.IsAny<MultiHopOptions>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new MultiHopResult(
                    new List<RankedDocument>
                    {
                        new("Password reset successful", 0.85f, new Dictionary<string, string>()),
                        new("Login failed: session expired", 0.90f, new Dictionary<string, string>()),
                        new("Root cause: cache invalidation issue", 0.95f, new Dictionary<string, string>())
                    },
                    new List<HopTrace>
                    {
                        new(1, "password reset", 20, 5),
                        new(2, "login failure session", 15, 5),
                        new(3, "root cause cache", 10, 3)
                    },
                    3
                ));

            // Act
            var result = await _service.RetrieveAsync(query);

            // Assert
            Assert.Equal(RAGStrategy.MultiHop, result.StrategyUsed);
            Assert.Equal(3, result.Documents.Count);
            Assert.True(result.Metadata.ContainsKey("hops"));
            Assert.Equal(3, result.Metadata["hops"]);
        }

        [Fact]
        public async Task RetrieveAsync_WithMultiHopDisabled_ShouldUseSingleHop()
        {
            // Arrange
            var query = "Why did this complex thing happen?";
            var options = new AgenticRAGOptions(EnableMultiHop: false);
            var embedding = new float[] { 0.1f, 0.2f, 0.3f };

            _embeddingServiceMock
                .Setup(x => x.GenerateEmbeddingAsync(query))
                .ReturnsAsync(embedding);

            _vectorDbMock
                .Setup(x => x.SearchAsync(
                    It.IsAny<string>(),
                    It.IsAny<float[]>(),
                    It.IsAny<int>()))
                .ReturnsAsync(new List<SearchResult>
                {
                    new() { Id = "1", Score = 0.8, Payload = new Dictionary<string, object> { ["Content"] = "Some result" } }
                });

            _rerankerMock
                .Setup(x => x.RerankAsync(
                    query,
                    It.IsAny<List<RetrievedDocument>>(),
                    It.IsAny<int>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(new List<RankedDocument>
                {
                    new("Some result", 0.85f, new Dictionary<string, string>())
                });

            // Act
            var result = await _service.RetrieveAsync(query, options);

            // Assert
            Assert.Equal(RAGStrategy.SingleHop, result.StrategyUsed);
            _multiHopRetrieverMock.Verify(
                x => x.RetrieveAsync(It.IsAny<string>(), It.IsAny<MultiHopOptions>(), It.IsAny<CancellationToken>()),
                Times.Never
            );
        }

        [Fact]
        public async Task RetrieveAsync_ShouldRespectMaxDocumentsLimit()
        {
            // Arrange
            var query = "test query";
            var options = new AgenticRAGOptions(MaxDocuments: 5);
            var embedding = new float[] { 0.1f, 0.2f, 0.3f };

            _embeddingServiceMock
                .Setup(x => x.GenerateEmbeddingAsync(query))
                .ReturnsAsync(embedding);

            _vectorDbMock
                .Setup(x => x.SearchAsync(
                    It.IsAny<string>(),
                    It.IsAny<float[]>(),
                    It.IsAny<int>()))
                .ReturnsAsync(Enumerable.Range(1, 20)
                    .Select(i => new SearchResult
                    {
                        Id = i.ToString(),
                        Score = 0.5 + (i * 0.01),
                        Payload = new Dictionary<string, object> { ["Content"] = $"Doc {i}" }
                    })
                    .ToList());

            _rerankerMock
                .Setup(x => x.RerankAsync(
                    query,
                    It.IsAny<List<RetrievedDocument>>(),
                    It.IsAny<int>(),
                    It.IsAny<CancellationToken>()))
                .Returns<string, List<RetrievedDocument>, int, CancellationToken>((q, candidates, topK, ct) =>
                    Task.FromResult(candidates.Take(topK).Select(c => new RankedDocument(c.Content, 0.9f, c.Metadata)).ToList())
                );

            // Act
            var result = await _service.RetrieveAsync(query, options);

            // Assert
            Assert.True(result.Documents.Count <= options.MaxDocuments);
        }
    }
}
