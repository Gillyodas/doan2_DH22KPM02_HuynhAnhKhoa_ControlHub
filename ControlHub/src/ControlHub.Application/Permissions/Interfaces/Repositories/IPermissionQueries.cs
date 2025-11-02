using ControlHub.Domain.Permissions;

namespace ControlHub.Application.Permissions.Interfaces.Repositories
{
    public interface IPermissionQueries
    {
        Task<Permission?> GetByIdAsync(Guid id, CancellationToken cancellationToken);
        Task<IEnumerable<Permission>> GetAllAsync(CancellationToken cancellationToken);
        Task<IEnumerable<Permission>> SearchByCodeAsync(string code, CancellationToken cancellationToken);
        Task<bool> ExistAsync(Guid id, CancellationToken cancellationToken);
        Task<IEnumerable<Permission>> GetByIdsAsync(IEnumerable<Guid> permissionIds, CancellationToken cancellationToken);
    }
}