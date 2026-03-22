using ControlHub.Domain.SharedKernel;

namespace ControlHub.Domain.Identity.Events
{
    public sealed record IdentifierAddedEvent(Guid AccountId, string IdentifierType) : IDomainEvent
    {
        public DateTime OccurredOn { get; } = DateTime.UtcNow;
    }
}
