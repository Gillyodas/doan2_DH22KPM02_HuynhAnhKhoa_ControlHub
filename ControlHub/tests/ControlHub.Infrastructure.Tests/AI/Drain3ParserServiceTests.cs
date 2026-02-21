using ControlHub.Application.Common.Logging;
using ControlHub.Infrastructure.AI.Parsing;
using FluentAssertions;

namespace ControlHub.Infrastructure.Tests.AI
{
    public class Drain3ParserServiceTests
    {
        private readonly Drain3ParserService _parser;

        public Drain3ParserServiceTests()
        {
            _parser = new Drain3ParserService(depth: 4, similarityThreshold: 0.5);
        }

        [Fact]
        public async Task ParseLogsAsync_ShouldClusterSimiliarLogs()
        {
            // Arrange
            var logs = new List<LogEntry>
            {
                new LogEntry { RenderedMessage = "Connection from 192.168.1.1 failed", Timestamp = DateTime.Now, Level = "Error" },
                new LogEntry { RenderedMessage = "Connection from 10.0.0.5 failed", Timestamp = DateTime.Now.AddSeconds(1), Level = "Error" },
                new LogEntry { RenderedMessage = "User admin logged in", Timestamp = DateTime.Now.AddSeconds(2), Level = "Info" }
            };

            // Act
            var result = await _parser.ParseLogsAsync(logs);

            // Assert
            result.Should().NotBeNull();
            result.Templates.Should().HaveCount(2); // Should find 2 templates: "Connection from <IP> failed" and "User admin logged in"

            var connectionTemplate = result.Templates.Find(t => t.Pattern.Contains("Connection"));
            connectionTemplate.Should().NotBeNull();
            connectionTemplate.Count.Should().Be(2);
            connectionTemplate.Pattern.Should().Contain("<IP>");
        }

        [Fact]
        public async Task ParseLogsAsync_ShouldMaskNumbersAndGuids()
        {
            // Arrange
            var logs = new List<LogEntry>
            {
                new LogEntry { RenderedMessage = "Process 1234 terminated with error 500", Timestamp = DateTime.Now, Level = "Error" },
                new LogEntry { RenderedMessage = "Process 5678 terminated with error 500", Timestamp = DateTime.Now, Level = "Error" }
            };

            // Act
            var result = await _parser.ParseLogsAsync(logs);

            // Assert
            result.Templates.Should().HaveCount(1);
            result.Templates[0].Pattern.Should().Contain("<NUM>");
            result.Templates[0].Count.Should().Be(2);
        }
    }
}
