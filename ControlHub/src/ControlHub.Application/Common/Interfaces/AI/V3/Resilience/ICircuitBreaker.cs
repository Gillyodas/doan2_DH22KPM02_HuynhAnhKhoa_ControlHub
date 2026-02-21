namespace ControlHub.Application.Common.Interfaces.AI.V3.Resilience
{
    /// <summary>
    /// Circuit Breaker interface - Protects services from cascading failures.
    /// States: Closed → Open → Half-Open → Closed
    /// </summary>
    public interface ICircuitBreaker
    {
        /// <summary>Current state of the circuit</summary>
        CircuitState State { get; }

        /// <summary>Execute action with circuit breaker protection</summary>
        Task<T> ExecuteAsync<T>(Func<CancellationToken, Task<T>> action, CancellationToken ct = default);

        /// <summary>Record a success (closes circuit if half-open)</summary>
        void RecordSuccess();

        /// <summary>Record a failure (may open circuit)</summary>
        void RecordFailure();

        /// <summary>Force circuit to open</summary>
        void Trip();

        /// <summary>Force circuit to close</summary>
        void Reset();
    }

    /// <summary>
    /// Circuit breaker states.
    /// </summary>
    public enum CircuitState
    {
        /// <summary>Circuit is closed, requests flow normally</summary>
        Closed,

        /// <summary>Circuit is open, requests fail fast</summary>
        Open,

        /// <summary>Circuit is testing, limited requests allowed</summary>
        HalfOpen
    }

    /// <summary>
    /// Circuit breaker options.
    /// </summary>
    public record CircuitBreakerOptions(
        /// <summary>Failures before opening circuit (default: 5)</summary>
        int FailureThreshold = 5,

        /// <summary>Success count in half-open to close (default: 2)</summary>
        int SuccessThreshold = 2,

        /// <summary>Time to wait before trying half-open (default: 30s)</summary>
        TimeSpan OpenDuration = default
    )
    {
        public TimeSpan OpenDuration { get; init; } = OpenDuration == default
            ? TimeSpan.FromSeconds(30)
            : OpenDuration;
    }
}
