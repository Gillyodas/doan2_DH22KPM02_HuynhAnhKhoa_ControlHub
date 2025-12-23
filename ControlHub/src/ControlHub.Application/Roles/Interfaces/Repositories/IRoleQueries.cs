using ControlHub.Application.Common.DTOs;
using ControlHub.Domain.Roles;

namespace ControlHub.Application.Roles.Interfaces.Repositories
{
    public interface IRoleQueries
    {
        Task<Role> GetByIdAsync(Guid id, CancellationToken cancellationToken);
        Task<IEnumerable<Role>> GetAllAsync(CancellationToken cancellationToken);
        Task<IReadOnlyList<Guid>> GetPermissionIdsByRoleIdAsync(Guid roleId, CancellationToken cancellationToken);
        Task<IEnumerable<Role>> SearchByNameAsync(string name, CancellationToken cancellationToken);
        Task<bool> ExistAsync(Guid roleId, CancellationToken cancellationToken);
        Task<PagedResult<Role>> SearchPaginationAsync(int pageIndex, int pageSize, string[] conditions, CancellationToken cancellationToken);
    }
}
