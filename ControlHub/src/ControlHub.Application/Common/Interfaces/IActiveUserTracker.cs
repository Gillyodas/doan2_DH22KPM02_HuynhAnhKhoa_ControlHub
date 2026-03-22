namespace ControlHub.Application.Common.Interfaces
{
    public interface IActiveUserTracker
    {
        int GetActiveCount();
        int Increment();
        int Decrement();
    }
}
