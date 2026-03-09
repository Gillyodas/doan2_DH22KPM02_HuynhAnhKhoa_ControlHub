using MediatR;

namespace ControlHub.Application.AccessControl.Events
{
    public record PermissionCreatedEvent : INotification
    {
        public Guid PermissionId { get; init; }
        public string PermissionName { get; init; } = string.Empty;
        public DateTimeOffset Timestamp { get; init; } = DateTimeOffset.UtcNow;
    }
}
