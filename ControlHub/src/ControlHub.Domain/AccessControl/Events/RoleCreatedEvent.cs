using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ControlHub.Domain.SharedKernel;

namespace ControlHub.Domain.AccessControl.Events
{
    public sealed record RoleCreatedEvent(Guid RoleId) : IDomainEvent
    {
        public DateTime OccurredOn { get; } = DateTime.UtcNow;
    }
}
