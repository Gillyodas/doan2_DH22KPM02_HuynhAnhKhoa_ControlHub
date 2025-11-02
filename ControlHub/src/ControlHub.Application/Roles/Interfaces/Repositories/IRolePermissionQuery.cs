using ControlHub.Application.Roles.DTOs;
using ControlHub.Domain.Roles;

namespace ControlHub.Application.Roles.Interfaces.Repositories
{
    public interface IRolePermissionQuery
    {
        Task<IEnumerable<RolePermission>> GetAllAsync(CancellationToken cancellationToken);
        Task<IEnumerable<RolePermissionDetailDto>> GetAllWithNameAsync(CancellationToken cancellationToken);
    }
}
