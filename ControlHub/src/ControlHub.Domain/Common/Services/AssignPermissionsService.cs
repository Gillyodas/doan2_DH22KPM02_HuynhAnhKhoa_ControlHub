using ControlHub.Domain.Permissions;
using ControlHub.Domain.Roles;
using ControlHub.SharedKernel.Permissions;
using ControlHub.SharedKernel.Results;

namespace ControlHub.Domain.Common.Services
{
    public class AssignPermissionsService
    {
        public Result Handle(
            Role role,
            IEnumerable<Permission> validPermissions)
        {
            if (!validPermissions.Any())
                return Result.Failure(PermissionErrors.PermissionNotFoundValid);

            foreach (var per in validPermissions)
            {
                role.AddPermission(per);
            }

            return Result.Success();
        }
    }
}
