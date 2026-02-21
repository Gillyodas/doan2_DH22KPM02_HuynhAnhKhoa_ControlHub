using ControlHub.Application.Common.Interfaces.AI.V3.Resilience;
using Microsoft.Extensions.Logging;

namespace ControlHub.Application.AI.V3.Resilience
{
    /// <summary>
    /// Circuit Breaker implementation - Protects ONNX/LLM services from cascading failures.
    /// </summary>
    public class CircuitBreaker : ICircuitBreaker
    {
        private readonly ILogger<CircuitBreaker> _logger;
        private readonly CircuitBreakerOptions _options;
        private readonly object _lock = new();

        private CircuitState _state = CircuitState.Closed;
        private int _failureCount;
        private int _successCount;
        private DateTime _lastFailureTime;

        public CircuitState State => _state;

        public CircuitBreaker(ILogger<CircuitBreaker> logger, CircuitBreakerOptions? options = null)
        {
            _logger = logger;
            _options = options ?? new CircuitBreakerOptions();
        }

        public async Task<T> ExecuteAsync<T>(Func<CancellationToken, Task<T>> action, CancellationToken ct = default)
        {
            lock (_lock)
            {
                // Check if we should transition from Open to HalfOpen
                if (_state == CircuitState.Open)
                {
                    if (DateTime.UtcNow - _lastFailureTime >= _options.OpenDuration)
                    {
                        _state = CircuitState.HalfOpen;
                        _successCount = 0;
                        _logger.LogInformation("Circuit breaker transitioning to HalfOpen");
                    }
                    else
                    {
                        _logger.LogWarning("Circuit breaker is Open, failing fast");
                        throw new CircuitBreakerOpenException("Circuit breaker is open");
                    }
                }
            }

            try
            {
                var result = await action(ct);
                RecordSuccess();
                return result;
            }
            catch (Exception ex) when (ex is not CircuitBreakerOpenException)
            {
                RecordFailure();
                throw;
            }
        }

        public void RecordSuccess()
        {
            lock (_lock)
            {
                _failureCount = 0;

                if (_state == CircuitState.HalfOpen)
                {
                    _successCount++;
                    if (_successCount >= _options.SuccessThreshold)
                    {
                        _state = CircuitState.Closed;
                        _logger.LogInformation("Circuit breaker closed after {Count} successes", _successCount);
                    }
                }
            }
        }

        public void RecordFailure()
        {
            lock (_lock)
            {
                _failureCount++;
                _lastFailureTime = DateTime.UtcNow;

                if (_state == CircuitState.HalfOpen)
                {
                    _state = CircuitState.Open;
                    _logger.LogWarning("Circuit breaker opened from HalfOpen due to failure");
                }
                else if (_failureCount >= _options.FailureThreshold)
                {
                    _state = CircuitState.Open;
                    _logger.LogWarning("Circuit breaker opened after {Count} failures", _failureCount);
                }
            }
        }

        public void Trip()
        {
            lock (_lock)
            {
                _state = CircuitState.Open;
                _lastFailureTime = DateTime.UtcNow;
                _logger.LogWarning("Circuit breaker manually tripped");
            }
        }

        public void Reset()
        {
            lock (_lock)
            {
                _state = CircuitState.Closed;
                _failureCount = 0;
                _successCount = 0;
                _logger.LogInformation("Circuit breaker manually reset");
            }
        }
    }

    /// <summary>
    /// Exception thrown when circuit breaker is open.
    /// </summary>
    public class CircuitBreakerOpenException : Exception
    {
        public CircuitBreakerOpenException(string message) : base(message) { }
    }
}
