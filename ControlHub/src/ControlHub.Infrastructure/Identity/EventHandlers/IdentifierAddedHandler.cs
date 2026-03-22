using ControlHub.Application.Common.Interfaces;
using ControlHub.Domain.Identity.Events;
using ControlHub.Infrastructure.RealTime.Hubs;
using MediatR;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;

namespace ControlHub.Infrastructure.Identity.EventHandlers
{
    internal class IdentifierAddedHandler : INotificationHandler<IdentifierAddedEvent>
    {
        private readonly IHubContext<DashboardHub> _hubContext;
        private readonly IDashboardStatsProvider _statsProvider;
        private readonly ILogger<IdentifierAddedHandler> _logger;

        public IdentifierAddedHandler(
            IHubContext<DashboardHub> hubContext,
            IDashboardStatsProvider statsProvider,
            ILogger<IdentifierAddedHandler> logger)
        {
            _hubContext = hubContext;
            _statsProvider = statsProvider;
            _logger = logger;
        }

        public async Task Handle(IdentifierAddedEvent notification, CancellationToken cancellationToken)
        {
            var stats = await _statsProvider.GetStatsAsync(cancellationToken);
            await _hubContext.Clients.All.SendAsync("DashboardStatsUpdated", stats, cancellationToken);

            _logger.LogInformation(
                "IdentifierAddedEvent: broadcast stats, AccountId={AccountId}, Type={Type}",
                notification.AccountId, notification.IdentifierType);
        }
    }
}
