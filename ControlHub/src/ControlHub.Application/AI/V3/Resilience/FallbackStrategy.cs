using Microsoft.Extensions.Logging;

namespace ControlHub.Application.AI.V3.Resilience
{
    /// <summary>
    /// Fallback Strategy - Graceful degradation V3 → V2.5 → V1.
    /// </summary>
    public interface IFallbackStrategy
    {
        /// <summary>Execute with fallback chain</summary>
        Task<T> ExecuteWithFallbackAsync<T>(
            Func<CancellationToken, Task<T>> primary,
            Func<CancellationToken, Task<T>>? secondary = null,
            Func<CancellationToken, Task<T>>? tertiary = null,
            CancellationToken ct = default
        );
    }

    /// <summary>
    /// Graceful degradation implementation.
    /// Tries V3 → V2.5 → V1 on failure.
    /// </summary>
    public class GracefulDegradation : IFallbackStrategy
    {
        private readonly ILogger<GracefulDegradation> _logger;

        public GracefulDegradation(ILogger<GracefulDegradation> logger)
        {
            _logger = logger;
        }

        public async Task<T> ExecuteWithFallbackAsync<T>(
            Func<CancellationToken, Task<T>> primary,
            Func<CancellationToken, Task<T>>? secondary = null,
            Func<CancellationToken, Task<T>>? tertiary = null,
            CancellationToken ct = default)
        {
            // Try primary (V3)
            try
            {
                _logger.LogDebug("Attempting primary (V3) execution");
                return await primary(ct);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Primary (V3) failed, attempting fallback");
            }

            // Try secondary (V2.5)
            if (secondary != null)
            {
                try
                {
                    _logger.LogInformation("Falling back to secondary (V2.5)");
                    return await secondary(ct);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Secondary (V2.5) failed, attempting tertiary");
                }
            }

            // Try tertiary (V1)
            if (tertiary != null)
            {
                try
                {
                    _logger.LogInformation("Falling back to tertiary (V1)");
                    return await tertiary(ct);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "All fallbacks failed");
                    throw;
                }
            }

            throw new InvalidOperationException("All execution strategies failed");
        }
    }

    /// <summary>
    /// Timeout handler for node execution.
    /// </summary>
    public static class TimeoutHandler
    {
        /// <summary>
        /// Execute with timeout.
        /// </summary>
        public static async Task<T> WithTimeoutAsync<T>(
            Func<CancellationToken, Task<T>> action,
            TimeSpan timeout,
            CancellationToken ct = default)
        {
            using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
            cts.CancelAfter(timeout);

            try
            {
                return await action(cts.Token);
            }
            catch (OperationCanceledException) when (!ct.IsCancellationRequested)
            {
                throw new TimeoutException($"Operation timed out after {timeout.TotalSeconds}s");
            }
        }

        /// <summary>
        /// Execute with timeout (void return).
        /// </summary>
        public static async Task WithTimeoutAsync(
            Func<CancellationToken, Task> action,
            TimeSpan timeout,
            CancellationToken ct = default)
        {
            await WithTimeoutAsync(async token =>
            {
                await action(token);
                return true;
            }, timeout, ct);
        }
    }
}
