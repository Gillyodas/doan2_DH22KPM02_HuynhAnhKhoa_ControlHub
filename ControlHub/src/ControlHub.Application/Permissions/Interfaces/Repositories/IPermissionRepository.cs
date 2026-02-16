using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace ControlHub.Application.Permissions.Interfaces.Repositories
{
    public interface IPermissionRepository
    {
        Task AddAsync(ControlHub.Domain.Permissions.Permission permission, CancellationToken cancellationToken);
        Task AddRangeAsync(IEnumerable<ControlHub.Domain.Permissions.Permission> permissions, CancellationToken cancellationToken);
        Task DeleteAsync(ControlHub.Domain.Permissions.Permission permission, CancellationToken cancellationToken);
        Task DeleteRangeAsync(IEnumerable<ControlHub.Domain.Permissions.Permission> permissions, CancellationToken cancellationToken);

        Task<IEnumerable<ControlHub.Domain.Permissions.Permission>> GetByIdsAsync(IEnumerable<Guid> permissionIds, CancellationToken cancellationToken);
        Task<ControlHub.Domain.Permissions.Permission?> GetByIdAsync(Guid id, CancellationToken cancellationToken);
    }
}
