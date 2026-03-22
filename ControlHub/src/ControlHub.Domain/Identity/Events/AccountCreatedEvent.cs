using ControlHub.Domain.SharedKernel;

namespace ControlHub.Domain.Identity.Events
{
    public sealed record AccountCreatedEvent(Guid AccountId, Guid RoleId) : IDomainEvent
    {
        public DateTime OccurredOn { get; } = DateTime.UtcNow;
    }
}
