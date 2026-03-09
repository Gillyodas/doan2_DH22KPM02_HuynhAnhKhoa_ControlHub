using MediatR;

namespace ControlHub.Application.Identity.Events
{
    public record IdentifierAddedEvent : INotification
    {
        public Guid AccountId { get; init; }
        public string IdentifierType { get; init; } = string.Empty;
        public DateTimeOffset Timestamp { get; init; } = DateTimeOffset.UtcNow;
    }
}
