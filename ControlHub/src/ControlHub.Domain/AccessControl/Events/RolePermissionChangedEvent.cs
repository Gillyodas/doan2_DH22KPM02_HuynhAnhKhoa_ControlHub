using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ControlHub.Domain.SharedKernel;

namespace ControlHub.Domain.AccessControl.Events
{
    /// <summary>
    /// Được raise khi permissions của một Role thay đổi
    /// (thêm, xóa, hoặc clear permissions).
    /// Handler: Invalidate IMemoryCache cho role tương ứng.
    /// </summary>
    public sealed record RolePermissionChangedEvent(Guid RoleId) : IDomainEvent
    {
        public DateTime OccurredOn { get; } = DateTime.UtcNow;
    }
}
