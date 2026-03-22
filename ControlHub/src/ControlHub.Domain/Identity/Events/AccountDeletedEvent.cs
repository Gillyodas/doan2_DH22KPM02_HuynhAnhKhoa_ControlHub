using ControlHub.Domain.SharedKernel;

namespace ControlHub.Domain.Identity.Events
{
    public sealed record AccountDeletedEvent(Guid AccountId) : IDomainEvent
    {
        public DateTime OccurredOn { get; } = DateTime.UtcNow;
    }
}
