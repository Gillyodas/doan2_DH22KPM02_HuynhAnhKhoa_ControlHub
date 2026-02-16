namespace ControlHub.Application.Roles.Interfaces.Repositories
{
    public interface IPermissionValidator
    {
        Task<List<Guid>> PermissionIdsExistAsync(IEnumerable<Guid> permissionIds, CancellationToken cancellationToken);
    }
}
