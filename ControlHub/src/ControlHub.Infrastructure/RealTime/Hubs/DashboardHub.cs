using ControlHub.Application.Common.Interfaces;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;

namespace ControlHub.Infrastructure.RealTime.Hubs
{
    public class DashboardHub : Hub
    {
        private readonly IActiveUserTracker _tracker;
        private readonly IDashboardStatsProvider _statsProvider;
        private readonly ILogger<DashboardHub> _logger;

        public DashboardHub(
            IActiveUserTracker tracker,
            IDashboardStatsProvider statsProvider,
            ILogger<DashboardHub> logger)
        {
            _tracker = tracker;
            _statsProvider = statsProvider;
            _logger = logger;
        }

        public override async Task OnConnectedAsync()
        {
            var count = _tracker.Increment();
            var userId = Context.User?.FindFirst("sub")?.Value ?? "Unknown";

            _logger.LogInformation("Dashboard connected: {UserId}. Active: {Count}", userId, count);
            await Clients.All.SendAsync("ActiveUsersUpdated", count);
            await base.OnConnectedAsync();
        }

        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            var count = _tracker.Decrement();
            _logger.LogInformation("Dashboard disconnected. Active: {Count}", count);
            await Clients.All.SendAsync("ActiveUsersUpdated", count);
            await base.OnDisconnectedAsync(exception);
        }

        public async Task RequestCurrentStats()
        {
            var stats = await _statsProvider.GetStatsAsync();
            await Clients.Caller.SendAsync("DashboardStatsUpdated", stats);
            await Clients.Caller.SendAsync("ActiveUsersUpdated", stats.ActiveUsers);
        }
    }
}
