using ControlHub.Application.AI.V3.Parsing;
using ControlHub.Application.Common.Interfaces.AI;
using ControlHub.Application.Common.Interfaces.AI.V3.Parsing;
using ControlHub.Application.Common.Logging;
using Microsoft.Extensions.Logging;
using Moq;

namespace ControlHub.Application.Tests.AI.V3
{
    public class HybridLogParserTests
    {
        private readonly Mock<ILogParserService> _drainParserMock = new();
        private readonly Mock<ISemanticLogClassifier> _semanticClassifierMock = new();
        private readonly Mock<ILogger<HybridLogParser>> _loggerMock = new();
        private readonly HybridLogParser _parser;

        public HybridLogParserTests()
        {
            _parser = new HybridLogParser(
                _drainParserMock.Object,
                _semanticClassifierMock.Object,
                _loggerMock.Object
            );
        }

        [Fact]
        public async Task ParseLogsAsync_WhenConfidenceIsHigh_ShouldUseDrain3()
        {
            // Arrange
            var log = new LogEntry { MessageTemplate = "System started", Timestamp = DateTime.UtcNow };
            var logs = new List<LogEntry> { log };

            var template = new LogTemplate("T1", "System started", 1, log.Timestamp, log.Timestamp, "Information");
            var drainResult = new LogParseResult(
                new List<LogTemplate> { template },
                new Dictionary<string, List<LogEntry>> { { "T1", new List<LogEntry> { log } } }
            );

            _drainParserMock.Setup(x => x.ParseLogsAsync(It.IsAny<List<LogEntry>>()))
                .ReturnsAsync(drainResult);

            // Act
            var result = await _parser.ParseLogsAsync(logs);

            // Assert
            Assert.Single(result.Templates);
            Assert.Equal("T1", result.Templates[0].TemplateId);
            Assert.Equal(1, result.Metadata.Drain3Count);
            Assert.Equal(0, result.Metadata.SemanticCount);
            _semanticClassifierMock.Verify(x => x.ClassifyAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact]
        public async Task ParseLogsAsync_WhenConfidenceIsLow_ShouldFallbackToSemantic()
        {
            // Arrange
            var log = new LogEntry { MessageTemplate = "Connection failed from 192.168.1.1", Timestamp = DateTime.UtcNow };
            var logs = new List<LogEntry> { log };

            // High wildcard count -> Low confidence heuristic (0.5f < 0.7f)
            var template = new LogTemplate("T1", "Connection failed from <*> <*> <*> <*>", 1, log.Timestamp, log.Timestamp, "Information");
            var drainResult = new LogParseResult(
                new List<LogTemplate> { template },
                new Dictionary<string, List<LogEntry>> { { "T1", new List<LogEntry> { log } } }
            );

            var semanticClassification = new LogClassification(
                "network",
                "connection_failure",
                0.9f,
                new Dictionary<string, string> { { "ip", "192.168.1.1" } }
            );

            _drainParserMock.Setup(x => x.ParseLogsAsync(It.IsAny<List<LogEntry>>()))
                .ReturnsAsync(drainResult);
            _semanticClassifierMock.Setup(x => x.ClassifyAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(semanticClassification);

            // Act
            var result = await _parser.ParseLogsAsync(logs);

            // Assert
            Assert.Single(result.Templates);
            Assert.StartsWith("semantic_", result.Templates[0].TemplateId);
            Assert.Equal("network", result.Templates[0].TemplateId.Replace("semantic_", ""));
            Assert.Equal(0, result.Metadata.Drain3Count);
            Assert.Equal(1, result.Metadata.SemanticCount);
        }

        [Fact]
        public async Task ParseSingleAsync_WhenHeuristicIsLow_ShouldUseSemantic()
        {
            // Arrange
            var logLine = "Database error at sector 5";
            var logEntry = new LogEntry { MessageTemplate = logLine, Timestamp = DateTime.UtcNow };

            var template = new LogTemplate("T1", "Database error at <*> <*> <*>", 1, DateTime.UtcNow, DateTime.UtcNow, "Error");
            var drainResult = new LogParseResult(
                new List<LogTemplate> { template },
                new Dictionary<string, List<LogEntry>> { { "T1", new List<LogEntry> { logEntry } } }
            );

            _drainParserMock.Setup(x => x.ParseLogsAsync(It.IsAny<List<LogEntry>>()))
                .ReturnsAsync(drainResult);
            _semanticClassifierMock.Setup(x => x.ClassifyAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new LogClassification("database", "disk_error", 0.95f, new Dictionary<string, string>()));

            // Act
            var result = await _parser.ParseSingleAsync(logLine);

            // Assert
            Assert.Equal(ParsingMethod.Semantic, result.Method);
            Assert.Equal("database", result.Classification.Category);
            Assert.Equal(0.95f, result.Confidence);
        }
    }
}
