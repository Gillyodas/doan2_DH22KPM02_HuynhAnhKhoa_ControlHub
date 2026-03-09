using ControlHub.Application.Identity.Events;
using MediatR;
using Microsoft.Extensions.Logging;

namespace ControlHub.Application.Identity.EventHandlers
{
    public class RoleAssignedToAccountEventHandler : INotificationHandler<RoleAssignedToAccountEvent>
    {
        private readonly ILogger<RoleAssignedToAccountEventHandler> _logger;

        public RoleAssignedToAccountEventHandler(ILogger<RoleAssignedToAccountEventHandler> logger)
        {
            _logger = logger;
        }

        public Task Handle(RoleAssignedToAccountEvent notification, CancellationToken cancellationToken)
        {
            _logger.LogInformation(
                "RoleAssigned | AccountId: {AccountId} | RoleId: {RoleId} | Timestamp: {Timestamp}",
                notification.AccountId,
                notification.RoleId,
                notification.Timestamp);

            return Task.CompletedTask;
        }
    }
}
