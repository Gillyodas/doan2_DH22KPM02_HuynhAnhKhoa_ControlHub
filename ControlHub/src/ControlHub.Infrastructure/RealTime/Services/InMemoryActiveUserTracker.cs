using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ControlHub.Infrastructure.RealTime.Services
{
    /// <summary>
    /// Default implementation - ch?y trên single instance.
    /// Ð? scale multi-instance: T?o RedisActiveUserTracker implements IActiveUserTracker.
    /// </summary>
    internal class InMemoryActiveUserTracker : IActiveUserTracker
    {
        private int _count = 0;
        public int Decrement()
        {
            var newValue = Interlocked.Decrement(ref _count);
            if(newValue < 0)
            {
                Interlocked.Exchange(ref _count, 0);
                return 0;
            }
            return newValue;
        }

        public int GetActiveCount() => _count;

        public int Increment() => Interlocked.Increment(ref _count);
    }
}
