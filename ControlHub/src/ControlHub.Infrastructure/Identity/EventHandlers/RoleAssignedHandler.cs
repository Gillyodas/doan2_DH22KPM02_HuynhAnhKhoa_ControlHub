using ControlHub.Domain.Identity.Events;
using ControlHub.Infrastructure.RealTime.Hubs;
using MediatR;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;

namespace ControlHub.Infrastructure.Identity.EventHandlers
{
    internal class RoleAssignedHandler : INotificationHandler<RoleAssignedToAccountEvent>
    {
        private readonly IHubContext<DashboardHub> _hubContext;
        private readonly ILogger<RoleAssignedHandler> _logger;

        public RoleAssignedHandler(IHubContext<DashboardHub> hubContext, ILogger<RoleAssignedHandler> logger)
        {
            _hubContext = hubContext;
            _logger = logger;
        }

        public async Task Handle(RoleAssignedToAccountEvent notification, CancellationToken cancellationToken)
        {
            await _hubContext.Clients.All.SendAsync(
                "RoleAssigned",
                new { notification.AccountId, notification.RoleId, notification.OccurredOn },
                cancellationToken);

            _logger.LogInformation(
                "RoleAssignedToAccountEvent: AccountId={AccountId}, RoleId={RoleId}",
                notification.AccountId, notification.RoleId);
        }
    }
}
