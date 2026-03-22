using ControlHub.Application.Common.Interfaces;
using ControlHub.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace ControlHub.Infrastructure.RealTime.Services
{
    internal class DashboardStatsProvider : IDashboardStatsProvider
    {
        private readonly AppDbContext _dbContext;
        private readonly IActiveUserTracker _activeUserTracker;
        private readonly ILogger<DashboardStatsProvider> _logger;

        public DashboardStatsProvider(
            AppDbContext dbContext,
            IActiveUserTracker activeUserTracker,
            ILogger<DashboardStatsProvider> logger)
        {
            _dbContext = dbContext;
            _activeUserTracker = activeUserTracker;
            _logger = logger;
        }

        public async Task<DashboardStats> GetStatsAsync(CancellationToken cancellationToken = default)
        {
            var totalRoles = await _dbContext.Roles
                .CountAsync(r => !r.IsDeleted, cancellationToken);

            var totalIdentifiers = await _dbContext.IdentifierConfigs
                .CountAsync(cancellationToken);

            var activeUsers = _activeUserTracker.GetActiveCount();

            _logger.LogDebug(
                "Dashboard stats: Roles={TotalRoles}, Identifiers={TotalIdentifiers}, ActiveUsers={ActiveUsers}",
                totalRoles, totalIdentifiers, activeUsers);

            return new DashboardStats(totalRoles, totalIdentifiers, activeUsers);
        }
    }
}
