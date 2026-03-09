namespace ControlHub.Application.AccessControl.Interfaces
{
    public interface IPermissionValidator
    {
        Task<List<Guid>> PermissionIdsExistAsync(IEnumerable<Guid> permissionIds, CancellationToken cancellationToken);
    }
}
