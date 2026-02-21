using MediatR;

namespace ControlHub.Application.Common.Events
{
    public record LoginAttemptedEvent : INotification
    {
        public DateTimeOffset Timestamp { get; init; } = DateTimeOffset.UtcNow;
        public bool IsSuccess { get; init; }
        public string IdentifierType { get; init; } = string.Empty;
        public string MaskedIdentifier { get; init; } = string.Empty;
        public string? FailureReason { get; init; }
    }
}
