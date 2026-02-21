using System.Text.Json;
using ControlHub.Application.Common.Logging;
using ControlHub.Infrastructure.Logging;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;

namespace ControlHub.Infrastructure.Tests.Logging
{
    public class LogReaderServiceTests : IDisposable
    {
        private readonly Mock<ILogger<LogReaderService>> _loggerMock;
        private readonly Mock<IConfiguration> _configurationMock;
        private readonly string _testLogDirectory;

        public LogReaderServiceTests()
        {
            _loggerMock = new Mock<ILogger<LogReaderService>>();
            _configurationMock = new Mock<IConfiguration>();

            // Create a unique temporary directory for tests
            _testLogDirectory = Path.Combine(Path.GetTempPath(), "ControlHubTests", Guid.NewGuid().ToString());
            Directory.CreateDirectory(_testLogDirectory);

            // Mock configuration to return the test directory
            _configurationMock.Setup(c => c["Logging:LogDirectory"]).Returns(_testLogDirectory);
        }

        [Fact]
        public async Task GetLogsByCorrelationIdAsync_ShouldReturnLogs_WhenCorrelationIdMatches()
        {
            // Arrange
            var correlationId = "test-correlation-id";
            var logEntry = new LogEntry
            {
                Timestamp = DateTime.UtcNow,
                RenderedMessage = "Test log message",
                Level = "Information",
                Properties = new Dictionary<string, object>
                {
                    { "CorrelationId", correlationId }
                }
            };

            var logFilePath = Path.Combine(_testLogDirectory, "log-20260122.json");
            var jsonLine = JsonSerializer.Serialize(logEntry);
            await File.WriteAllTextAsync(logFilePath, jsonLine);

            var service = new LogReaderService(_loggerMock.Object, _configurationMock.Object);

            // Act
            var result = await service.GetLogsByCorrelationIdAsync(correlationId);

            // Assert
            result.Should().NotBeEmpty();
            result.Should().HaveCount(1);
            result.First().Message.Should().Be("Test log message");
        }

        [Fact]
        public async Task GetLogsByCorrelationIdAsync_ShouldReturnEmpty_WhenNoLogsMatch()
        {
            // Arrange
            var correlationId = "test-correlation-id";
            var logEntry = new LogEntry
            {
                Timestamp = DateTime.UtcNow,
                RenderedMessage = "Other log message",
                Level = "Information",
                Properties = new Dictionary<string, object>
                {
                    { "CorrelationId", "other-id" }
                }
            };

            var logFilePath = Path.Combine(_testLogDirectory, "log-20260122.json");
            var jsonLine = JsonSerializer.Serialize(logEntry);
            await File.WriteAllTextAsync(logFilePath, jsonLine);

            var service = new LogReaderService(_loggerMock.Object, _configurationMock.Object);

            // Act
            var result = await service.GetLogsByCorrelationIdAsync(correlationId);

            // Assert
            result.Should().BeEmpty();
        }

        [Fact]
        public async Task GetLogsByCorrelationIdAsync_ShouldReturnEmpty_WhenDirectoryDoesNotExist()
        {
            // Arrange
            // Delete the directory created in constructor
            if (Directory.Exists(_testLogDirectory))
            {
                Directory.Delete(_testLogDirectory, true);
            }

            var service = new LogReaderService(_loggerMock.Object, _configurationMock.Object);

            // Act
            var result = await service.GetLogsByCorrelationIdAsync("any-id");

            // Assert
            result.Should().BeEmpty();
        }

        public void Dispose()
        {
            if (Directory.Exists(_testLogDirectory))
            {
                try
                {
                    Directory.Delete(_testLogDirectory, true);
                }
                catch
                {
                    // Ignore cleanup errors
                }
            }
        }
    }
}
