using ControlHub.Application.Common.Interfaces.AI;
using ControlHub.Infrastructure.AI.Strategies;
using FluentAssertions;

namespace ControlHub.Infrastructure.Tests.AI
{
    public class WeightedReservoirSamplingTests
    {
        private readonly WeightedReservoirSamplingStrategy _strategy;

        public WeightedReservoirSamplingTests()
        {
            _strategy = new WeightedReservoirSamplingStrategy();
        }

        [Fact]
        public void Sample_ShouldPrioritizeErrors()
        {
            // Arrange
            var templates = new List<LogTemplate>();
            // Add 90 Info logs
            for (int i = 0; i < 90; i++)
            {
                templates.Add(new LogTemplate($"Info_{i}", $"User activity {i}", 100, DateTime.Now, DateTime.Now, "Information"));
            }
            // Add 10 Error logs
            for (int i = 0; i < 10; i++)
            {
                templates.Add(new LogTemplate($"Error_{i}", $"Critical Failure {i}", 1, DateTime.Now, DateTime.Now, "Error"));
            }

            // Act
            var sampled = _strategy.Sample(templates, maxCount: 20);

            // Assert
            sampled.Should().HaveCount(20);

            // Errors should be highly represented due to severity weight
            var errorCount = sampled.Count(t => t.Severity == "Error");
            errorCount.Should().BeGreaterThan(5); // Expect at least some errors to make it through
        }
    }
}
