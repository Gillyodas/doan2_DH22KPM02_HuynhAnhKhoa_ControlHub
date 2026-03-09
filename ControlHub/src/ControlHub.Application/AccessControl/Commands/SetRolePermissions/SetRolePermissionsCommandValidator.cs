using FluentValidation;

namespace ControlHub.Application.AccessControl.Commands.SetRolePermissions
{
    public class SetRolePermissionsCommandValidator : AbstractValidator<SetRolePermissionsCommand>
    {
        public SetRolePermissionsCommandValidator()
        {
            RuleFor(x => x.RoleId).NotEmpty().WithMessage("Role ID is required.");
            RuleFor(x => x.PermissionIds).NotNull().WithMessage("Permission IDs list cannot be null.");
        }
    }
}
