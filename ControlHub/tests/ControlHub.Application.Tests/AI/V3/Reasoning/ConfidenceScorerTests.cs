using ControlHub.Application.AI.V3.Reasoning;
using ControlHub.Application.Common.Interfaces.AI.V3.RAG;
using ControlHub.Application.Common.Interfaces.AI.V3.Reasoning;
using Microsoft.Extensions.Logging;
using Moq;

namespace ControlHub.Application.Tests.AI.V3.Reasoning
{
    public class ConfidenceScorerTests
    {
        private readonly Mock<ILogger<ConfidenceScorer>> _loggerMock = new();
        private readonly ConfidenceScorer _scorer;

        public ConfidenceScorerTests()
        {
            _scorer = new ConfidenceScorer(_loggerMock.Object);
        }

        [Fact]
        public async Task ScoreAsync_WithHighQualityResult_ShouldReturnHighConfidence()
        {
            // Arrange
            var result = new ReasoningResult(
                Solution: "Reset the user's password and ensure session is cleared.",
                Explanation: "The login failure was caused by an expired password that wasn't properly reset.",
                Steps: new List<string> { "Step 1: Reset password", "Step 2: Clear sessions", "Step 3: Verify login" },
                Confidence: 0.85f
            );

            var context = new ReasoningContext(
                Query: "Why did login fail?",
                RetrievedDocs: new List<RankedDocument>
                {
                    new("Login failed: password expired", 0.9f, new Dictionary<string, string>()),
                    new("Password reset successful", 0.85f, new Dictionary<string, string>())
                }
            );

            // Act
            var score = await _scorer.ScoreAsync(result, context);

            // Assert
            Assert.True(score.Overall >= 0.7f, $"Expected high confidence, got {score.Overall}");
            Assert.True(score.GetLevel() == "High" || score.GetLevel() == "Very High", $"Expected High or Very High, got {score.GetLevel()}");
            Assert.True(score.IsConfident());
        }

        [Fact]
        public async Task ScoreAsync_WithLowQualityResult_ShouldReturnLowConfidence()
        {
            // Arrange
            var result = new ReasoningResult(
                Solution: "",
                Explanation: "",
                Steps: new List<string>(),
                Confidence: 0.2f
            );

            var context = new ReasoningContext(
                Query: "Some query",
                RetrievedDocs: new List<RankedDocument>()
            );

            // Act
            var score = await _scorer.ScoreAsync(result, context);

            // Assert
            Assert.True(score.Overall < 0.5f, $"Expected low confidence, got {score.Overall}");
            Assert.False(score.IsConfident());
        }

        [Fact]
        public async Task ScoreAsync_ShouldReturnJustification()
        {
            // Arrange
            var result = new ReasoningResult(
                Solution: "Test solution",
                Explanation: "Test explanation",
                Steps: new List<string> { "Step 1" },
                Confidence: 0.6f
            );

            var context = new ReasoningContext(
                Query: "Test query",
                RetrievedDocs: new List<RankedDocument>
                {
                    new("Test doc", 0.6f, new Dictionary<string, string>())
                }
            );

            // Act
            var score = await _scorer.ScoreAsync(result, context);

            // Assert
            Assert.False(string.IsNullOrEmpty(score.Justification));
        }

        [Fact]
        public async Task ConfidenceScore_GetLevel_ShouldReturnCorrectLevel()
        {
            // Arrange
            var veryHigh = new ConfidenceScore(0.95f, 0.9f, 0.9f, "test");
            var high = new ConfidenceScore(0.75f, 0.7f, 0.7f, "test");
            var medium = new ConfidenceScore(0.55f, 0.5f, 0.5f, "test");
            var low = new ConfidenceScore(0.35f, 0.3f, 0.3f, "test");
            var veryLow = new ConfidenceScore(0.15f, 0.1f, 0.1f, "test");

            // Assert
            Assert.Equal("Very High", veryHigh.GetLevel());
            Assert.Equal("High", high.GetLevel());
            Assert.Equal("Medium", medium.GetLevel());
            Assert.Equal("Low", low.GetLevel());
            Assert.Equal("Very Low", veryLow.GetLevel());
        }
    }
}
