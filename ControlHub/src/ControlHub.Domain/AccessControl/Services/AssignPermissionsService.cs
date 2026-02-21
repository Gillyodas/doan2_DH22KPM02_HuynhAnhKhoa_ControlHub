using ControlHub.Domain.AccessControl.Aggregates;
using ControlHub.Domain.AccessControl.Entities;
using ControlHub.SharedKernel.Permissions;
using ControlHub.SharedKernel.Results;

namespace ControlHub.Domain.AccessControl.Services
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
