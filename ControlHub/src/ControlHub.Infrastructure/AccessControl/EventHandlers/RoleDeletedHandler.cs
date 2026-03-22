using ControlHub.Application.Common.Interfaces;
using ControlHub.Domain.AccessControl.Events;
using ControlHub.Infrastructure.RealTime.Hubs;
using MediatR;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;

namespace ControlHub.Infrastructure.AccessControl.EventHandlers
{
    internal class RoleDeletedHandler : INotificationHandler<RoleDeletedEvent>
    {
        private readonly IMemoryCache _cache;
        private readonly IHubContext<DashboardHub> _hubContext;
        private readonly IDashboardStatsProvider _statsProvider;
        private readonly ILogger<RoleDeletedHandler> _logger;

        public RoleDeletedHandler(
            IMemoryCache cache,
            IHubContext<DashboardHub> hubContext,
            IDashboardStatsProvider statsProvider,
            ILogger<RoleDeletedHandler> logger)
        {
            _cache = cache;
            _hubContext = hubContext;
            _statsProvider = statsProvider;
            _logger = logger;
        }

        public async Task Handle(RoleDeletedEvent notification, CancellationToken cancellationToken)
        {
            _cache.Remove($"role-{notification.RoleId}");
            _cache.Remove($"role-perms-{notification.RoleId}");
            _cache.Remove("roles-all");

            var stats = await _statsProvider.GetStatsAsync(cancellationToken);
            await _hubContext.Clients.All.SendAsync("DashboardStatsUpdated", stats, cancellationToken);

            _logger.LogInformation(
                "RoleDeletedEvent: invalidated cache, broadcast stats, RoleId={RoleId}",
                notification.RoleId);
        }
    }
}
