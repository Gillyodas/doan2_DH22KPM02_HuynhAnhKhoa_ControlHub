namespace ControlHub.Application.Common.Interfaces
{
    public interface IDashboardStatsProvider
    {
        Task<DashboardStats> GetStatsAsync(CancellationToken cancellationToken = default);
    }

    public record DashboardStats(int TotalRoles, int TotalIdentifiers, int ActiveUsers);
}
