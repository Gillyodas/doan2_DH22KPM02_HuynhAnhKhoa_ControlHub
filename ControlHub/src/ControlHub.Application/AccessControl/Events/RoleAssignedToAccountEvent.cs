using MediatR;

namespace ControlHub.Application.AccessControl.Events
{
    public record RoleAssignedToAccountEvent : INotification
    {
        public Guid AccountId { get; init; }
        public Guid RoleId { get; init; }
        public DateTimeOffset Timestamp { get; init; } = DateTimeOffset.UtcNow;
    }
}
