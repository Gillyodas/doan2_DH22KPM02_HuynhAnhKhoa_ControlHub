using ControlHub.Application.Common.DTOs;
using ControlHub.Domain.Permissions;
using ControlHub.Domain.Roles;

namespace ControlHub.Application.Permissions.Interfaces.Repositories
{
    public interface IPermissionQueries
    {
        Task<Permission?> GetByIdAsync(Guid id, CancellationToken cancellationToken);
        Task<IEnumerable<Permission>> GetAllAsync(CancellationToken cancellationToken);
        Task<IEnumerable<Permission>> SearchByCodeAsync(string code, CancellationToken cancellationToken);
        Task<bool> ExistAsync(Guid id, CancellationToken cancellationToken);
        Task<IEnumerable<Permission>> GetByIdsAsync(IEnumerable<Guid> permissionIds, CancellationToken cancellationToken);
        Task<IEnumerable<Permission>> GetByRoleIdAsync(Guid roleId, CancellationToken cancellationToken);
        Task<PagedResult<Permission>> SearchPaginationAsync(int pageIndex, int pageSize, string[] conditions, CancellationToken cancellationToken);
    }
}