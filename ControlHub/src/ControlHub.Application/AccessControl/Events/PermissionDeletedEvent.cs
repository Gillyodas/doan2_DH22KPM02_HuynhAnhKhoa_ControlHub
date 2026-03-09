using MediatR;

namespace ControlHub.Application.AccessControl.Events
{
    public record PermissionDeletedEvent : INotification
    {
        public Guid PermissionId { get; init; }
        public DateTimeOffset Timestamp { get; init; } = DateTimeOffset.UtcNow;
    }
}
