using MediatR;

namespace ControlHub.Application.Identity.Events
{
    public record AccountRegisteredEvent : INotification
    {
        public Guid AccountId { get; init; }
        public string Role { get; init; } = string.Empty;
        public DateTimeOffset Timestamp { get; init; } = DateTimeOffset.UtcNow;
    }
}
