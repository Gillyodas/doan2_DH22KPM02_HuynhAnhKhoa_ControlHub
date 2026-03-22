using ControlHub.Domain.Identity.Events;
using ControlHub.Infrastructure.RealTime.Hubs;
using MediatR;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;

namespace ControlHub.Infrastructure.Identity.EventHandlers
{
    internal class AccountCreatedHandler : INotificationHandler<AccountCreatedEvent>
    {
        private readonly IHubContext<DashboardHub> _hubContext;
        private readonly ILogger<AccountCreatedHandler> _logger;

        public AccountCreatedHandler(IHubContext<DashboardHub> hubContext, ILogger<AccountCreatedHandler> logger)
        {
            _hubContext = hubContext;
            _logger = logger;
        }

        public async Task Handle(AccountCreatedEvent notification, CancellationToken cancellationToken)
        {
            await _hubContext.Clients.All.SendAsync(
                "AccountCreated",
                new { notification.AccountId, notification.RoleId, notification.OccurredOn },
                cancellationToken);

            _logger.LogInformation(
                "AccountCreatedEvent: AccountId={AccountId}, RoleId={RoleId}",
                notification.AccountId, notification.RoleId);
        }
    }
}
