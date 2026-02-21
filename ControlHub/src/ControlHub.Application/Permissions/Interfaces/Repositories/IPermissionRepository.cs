namespace ControlHub.Application.Permissions.Interfaces.Repositories
{
    public interface IPermissionRepository
    {
        Task AddAsync(ControlHub.Domain.AccessControl.Entities.Permission permission, CancellationToken cancellationToken);
        Task AddRangeAsync(IEnumerable<ControlHub.Domain.AccessControl.Entities.Permission> permissions, CancellationToken cancellationToken);
        Task DeleteAsync(ControlHub.Domain.AccessControl.Entities.Permission permission, CancellationToken cancellationToken);
        Task DeleteRangeAsync(IEnumerable<ControlHub.Domain.AccessControl.Entities.Permission> permissions, CancellationToken cancellationToken);

        Task<IEnumerable<ControlHub.Domain.AccessControl.Entities.Permission>> GetByIdsAsync(IEnumerable<Guid> permissionIds, CancellationToken cancellationToken);
        Task<ControlHub.Domain.AccessControl.Entities.Permission?> GetByIdAsync(Guid id, CancellationToken cancellationToken);
    }
}
