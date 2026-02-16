using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
