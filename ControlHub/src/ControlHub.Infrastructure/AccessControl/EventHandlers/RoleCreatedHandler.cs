using ControlHub.Application.AccessControl.Interfaces.Repositories;
using ControlHub.Application.Common.Interfaces;
using ControlHub.Domain.AccessControl.Events;
using ControlHub.Infrastructure.RealTime.Hubs;
using MediatR;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;

namespace ControlHub.Infrastructure.AccessControl.EventHandlers
{
    internal class RoleCreatedHandler : INotificationHandler<RoleCreatedEvent>
    {
        private readonly IMemoryCache _cache;
        private readonly IRoleQueries _roleQueries;
        private readonly IHubContext<DashboardHub> _hubContext;
        private readonly IDashboardStatsProvider _statsProvider;
        private readonly ILogger<RoleCreatedHandler> _logger;

        public RoleCreatedHandler(
            IMemoryCache cache,
            IRoleQueries roleQueries,
            IHubContext<DashboardHub> hubContext,
            IDashboardStatsProvider statsProvider,
            ILogger<RoleCreatedHandler> logger)
        {
            _cache = cache;
            _roleQueries = roleQueries;
            _hubContext = hubContext;
            _statsProvider = statsProvider;
            _logger = logger;
        }

        public async Task Handle(RoleCreatedEvent notification, CancellationToken cancellationToken)
        {
            _cache.Remove("roles-all");
            await _roleQueries.GetByIdAsync(notification.RoleId, cancellationToken);

            var stats = await _statsProvider.GetStatsAsync(cancellationToken);
            await _hubContext.Clients.All.SendAsync("DashboardStatsUpdated", stats, cancellationToken);

            _logger.LogInformation(
                "RoleCreatedEvent: invalidated cache, broadcast stats, RoleId={RoleId}",
                notification.RoleId);
        }
    }
}
