using MediatR;

namespace ControlHub.Application.Identity.Events
{
    public record AccountSignedOutEvent : INotification
    {
        public Guid AccountId { get; init; }
        public DateTimeOffset Timestamp { get; init; } = DateTimeOffset.UtcNow;
    }
}
