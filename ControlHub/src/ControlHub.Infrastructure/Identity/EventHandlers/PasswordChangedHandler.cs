using ControlHub.Domain.Identity.Events;
using ControlHub.Infrastructure.RealTime.Hubs;
using MediatR;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;

namespace ControlHub.Infrastructure.Identity.EventHandlers
{
    internal class PasswordChangedHandler : INotificationHandler<PasswordChangedEvent>
    {
        private readonly IHubContext<DashboardHub> _hubContext;
        private readonly ILogger<PasswordChangedHandler> _logger;

        public PasswordChangedHandler(IHubContext<DashboardHub> hubContext, ILogger<PasswordChangedHandler> logger)
        {
            _hubContext = hubContext;
            _logger = logger;
        }

        public async Task Handle(PasswordChangedEvent notification, CancellationToken cancellationToken)
        {
            await _hubContext.Clients.All.SendAsync(
                "PasswordChanged",
                new { notification.AccountId, notification.OccurredOn },
                cancellationToken);

            _logger.LogInformation(
                "PasswordChangedEvent: AccountId={AccountId}",
                notification.AccountId);
        }
    }
}
