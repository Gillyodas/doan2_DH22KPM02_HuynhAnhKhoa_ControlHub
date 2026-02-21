using ControlHub.Domain.AccessControl.Aggregates;
using ControlHub.Domain.AccessControl.Entities;
using ControlHub.SharedKernel.Results;

namespace ControlHub.Domain.AccessControl.Services
{
    public class CreateRoleWithPermissionsService
    {
        private readonly AssignPermissionsService _assignPermissionsService;
        public CreateRoleWithPermissionsService(
            AssignPermissionsService assignPermissionsService)
        {
            _assignPermissionsService = assignPermissionsService;
        }
        public Result<Role> Handle(
            string name,
            string description,
            IEnumerable<Permission> validPermissions)
        {
            var role = Role.Create(Guid.NewGuid(), name, description);

            var result = _assignPermissionsService.Handle(role, validPermissions);

            if (result.IsFailure)
                return Result<Role>.Failure(result.Error, result.Exception);

            return Result<Role>.Success(role);
        }
    }
}
