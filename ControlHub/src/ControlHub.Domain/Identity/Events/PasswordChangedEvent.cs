using ControlHub.Domain.SharedKernel;

namespace ControlHub.Domain.Identity.Events
{
    public sealed record PasswordChangedEvent(Guid AccountId) : IDomainEvent
    {
        public DateTime OccurredOn { get; } = DateTime.UtcNow;
    }
}
