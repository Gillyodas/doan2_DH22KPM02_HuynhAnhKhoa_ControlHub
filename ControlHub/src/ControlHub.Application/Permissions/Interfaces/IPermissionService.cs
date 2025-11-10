namespace ControlHub.Application.Permissions.Interfaces
{
    public interface IPermissionService
    {
        Task<IEnumerable<string>> GetPermissionsForRoleIdAsync(Guid roleId, CancellationToken cancellationToken);
    }
}
