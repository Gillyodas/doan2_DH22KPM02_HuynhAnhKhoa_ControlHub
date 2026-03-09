using MediatR;

namespace ControlHub.Application.AuditAI.Events
{
    public record AuditAnalysisCompletedEvent : INotification
    {
        public string Query { get; init; } = string.Empty;
        public bool IsSuccess { get; init; }
        public DateTimeOffset Timestamp { get; init; } = DateTimeOffset.UtcNow;
    }
}
