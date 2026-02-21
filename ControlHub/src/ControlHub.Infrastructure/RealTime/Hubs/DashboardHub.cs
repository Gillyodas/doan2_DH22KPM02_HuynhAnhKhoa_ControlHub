using ControlHub.Infrastructure.RealTime.Services;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;

namespace ControlHub.Infrastructure.RealTime.Hubs
{
    /// <summary>
    /// Hub cho Admin Dashboard.
    /// CH? Admin ho?c SupperAdmin m?i du?c connect.
    /// </summary>
    public class DashboardHub : Hub
    {
        private readonly IActiveUserTracker _tracker;
        private readonly ILogger<DashboardHub> _logger;
        //TODO: Format log

        public DashboardHub(IActiveUserTracker tracker, ILogger<DashboardHub> logger)
        {
            _tracker = tracker;
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
            var count = _tracker.GetActiveCount();
            await Clients.Caller.SendAsync("ActiveUsersUpdated", count);
        }
    }
}
