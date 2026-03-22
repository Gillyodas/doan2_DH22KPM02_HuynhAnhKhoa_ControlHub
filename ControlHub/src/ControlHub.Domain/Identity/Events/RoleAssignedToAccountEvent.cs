using ControlHub.Domain.SharedKernel;

namespace ControlHub.Domain.Identity.Events
{
    public sealed record RoleAssignedToAccountEvent(Guid AccountId, Guid RoleId) : IDomainEvent
    {
        public DateTime OccurredOn { get; } = DateTime.UtcNow;
    }
}
