using ControlHub.Application.AI.V3.Resilience;
using ControlHub.Application.Common.Interfaces.AI.V3.Resilience;
using Microsoft.Extensions.Logging;
using Moq;

namespace ControlHub.Application.Tests.AI.V3.Resilience
{
    public class CircuitBreakerTests
    {
        private readonly Mock<ILogger<CircuitBreaker>> _loggerMock = new();

        [Fact]
        public async Task ExecuteAsync_WhenClosed_ShouldExecuteAction()
        {
            // Arrange
            var breaker = new CircuitBreaker(_loggerMock.Object);
            var executed = false;

            // Act
            await breaker.ExecuteAsync(async ct =>
            {
                executed = true;
                return true;
            });

            // Assert
            Assert.True(executed);
            Assert.Equal(CircuitState.Closed, breaker.State);
        }

        [Fact]
        public void RecordFailure_ShouldOpenCircuitAfterThreshold()
        {
            // Arrange
            var options = new CircuitBreakerOptions(FailureThreshold: 3);
            var breaker = new CircuitBreaker(_loggerMock.Object, options);

            // Act
            breaker.RecordFailure();
            breaker.RecordFailure();
            Assert.Equal(CircuitState.Closed, breaker.State);

            breaker.RecordFailure();

            // Assert
            Assert.Equal(CircuitState.Open, breaker.State);
        }

        [Fact]
        public async Task ExecuteAsync_WhenOpen_ShouldThrowCircuitBreakerOpenException()
        {
            // Arrange
            var breaker = new CircuitBreaker(_loggerMock.Object);
            breaker.Trip();

            // Act & Assert
            await Assert.ThrowsAsync<CircuitBreakerOpenException>(async () =>
            {
                await breaker.ExecuteAsync<bool>(async ct => true);
            });
        }

        [Fact]
        public void Reset_ShouldCloseCircuit()
        {
            // Arrange
            var breaker = new CircuitBreaker(_loggerMock.Object);
            breaker.Trip();
            Assert.Equal(CircuitState.Open, breaker.State);

            // Act
            breaker.Reset();

            // Assert
            Assert.Equal(CircuitState.Closed, breaker.State);
        }
    }

    public class TimeoutHandlerTests
    {
        [Fact]
        public async Task WithTimeoutAsync_WhenCompletesInTime_ShouldReturnResult()
        {
            // Act
            var result = await TimeoutHandler.WithTimeoutAsync(
                async ct => { await Task.Delay(10, ct); return 42; },
                TimeSpan.FromSeconds(1)
            );

            // Assert
            Assert.Equal(42, result);
        }

        [Fact]
        public async Task WithTimeoutAsync_WhenTimesOut_ShouldThrowTimeoutException()
        {
            // Act & Assert
            await Assert.ThrowsAsync<TimeoutException>(async () =>
            {
                await TimeoutHandler.WithTimeoutAsync(
                    async ct => { await Task.Delay(5000, ct); return 42; },
                    TimeSpan.FromMilliseconds(50)
                );
            });
        }
    }
}
