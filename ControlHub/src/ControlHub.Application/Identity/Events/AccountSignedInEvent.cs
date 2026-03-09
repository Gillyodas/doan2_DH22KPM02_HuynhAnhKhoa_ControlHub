using MediatR;

namespace ControlHub.Application.Identity.Events
{
    public record AccountSignedInEvent : INotification
    {
        public DateTimeOffset Timestamp { get; init; } = DateTimeOffset.UtcNow;
        public bool IsSuccess { get; init; }
        public string IdentifierType { get; init; } = string.Empty;
        public string MaskedIdentifier { get; init; } = string.Empty;
        public string? FailureReason { get; init; }
    }
}
