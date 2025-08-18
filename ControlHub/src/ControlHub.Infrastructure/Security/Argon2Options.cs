using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ControlHub.Infrastructure.Security
{
    public sealed class Argon2Options
    {
        public int DegreeOfParallelism { get; init; } = Environment.ProcessorCount; // p
        public int MemorySizeKB { get; init; } = 65536; // m = 64MB
        public int Iterations { get; init; } = 3;       // t
        public int SaltSize { get; init; } = 16;        // bytes
        public int HashSize { get; init; } = 32;        // 256-bit
    }
}
