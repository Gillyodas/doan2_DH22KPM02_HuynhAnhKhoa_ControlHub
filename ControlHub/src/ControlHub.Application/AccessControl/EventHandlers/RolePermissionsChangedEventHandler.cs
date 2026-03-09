using ControlHub.Application.AccessControl.Events;
using MediatR;
using Microsoft.Extensions.Logging;

namespace ControlHub.Application.AccessControl.EventHandlers
{
    public class RolePermissionsChangedEventHandler : INotificationHandler<RolePermissionsChangedEvent>
    {
        private readonly ILogger<RolePermissionsChangedEventHandler> _logger;

        public RolePermissionsChangedEventHandler(ILogger<RolePermissionsChangedEventHandler> logger)
        {
            _logger = logger;
        }

        public Task Handle(RolePermissionsChangedEvent notification, CancellationToken cancellationToken)
        {
            _logger.LogInformation(
                "RolePermissionsChanged | RoleId: {RoleId} | Timestamp: {Timestamp}",
                notification.RoleId,
                notification.Timestamp);

            return Task.CompletedTask;
        }
    }
}
