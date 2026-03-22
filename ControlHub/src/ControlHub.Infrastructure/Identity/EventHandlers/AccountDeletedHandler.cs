using ControlHub.Domain.Identity.Events;
using ControlHub.Infrastructure.RealTime.Hubs;
using MediatR;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;

namespace ControlHub.Infrastructure.Identity.EventHandlers
{
    internal class AccountDeletedHandler : INotificationHandler<AccountDeletedEvent>
    {
        private readonly IHubContext<DashboardHub> _hubContext;
        private readonly ILogger<AccountDeletedHandler> _logger;

        public AccountDeletedHandler(IHubContext<DashboardHub> hubContext, ILogger<AccountDeletedHandler> logger)
        {
            _hubContext = hubContext;
            _logger = logger;
        }

        public async Task Handle(AccountDeletedEvent notification, CancellationToken cancellationToken)
        {
            await _hubContext.Clients.All.SendAsync(
                "AccountDeleted",
                new { notification.AccountId, notification.OccurredOn },
                cancellationToken);

            _logger.LogInformation(
                "AccountDeletedEvent: AccountId={AccountId}",
                notification.AccountId);
        }
    }
}
