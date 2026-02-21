namespace ControlHub.Infrastructure.RealTime.Services
{
    /// <summary>
    /// Interface cho phép swap implementation (InMemory -> Redis) mà không d?i code.
    /// </summary>
    public interface IActiveUserTracker
    {
        int GetActiveCount();
        int Increment();
        int Decrement();
    }
}
