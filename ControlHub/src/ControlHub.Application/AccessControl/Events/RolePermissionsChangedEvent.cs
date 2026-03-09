using MediatR;

namespace ControlHub.Application.AccessControl.Events
{
    public record RolePermissionsChangedEvent : INotification
    {
        public Guid RoleId { get; init; }
        public DateTimeOffset Timestamp { get; init; } = DateTimeOffset.UtcNow;
    }
}
