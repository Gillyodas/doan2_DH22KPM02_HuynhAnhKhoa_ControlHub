using ControlHub.Domain.SharedKernel;

namespace ControlHub.Domain.AccessControl.Events
{
    public sealed record RoleDeletedEvent(Guid RoleId) : IDomainEvent
    {
        public DateTime OccurredOn { get; } = DateTime.UtcNow;
    }
}
