using ControlHub.Domain.AccessControl.Entities;
using ControlHub.SharedKernel.Permissions;
using FluentValidation;

namespace ControlHub.Application.Permissions.Commands.UpdatePermission
{
    public class UpdatePermissionCommandValidator : AbstractValidator<UpdatePermissionCommand>
    {
        public UpdatePermissionCommandValidator()
        {
            RuleFor(x => x.Id)
                .NotEmpty().WithMessage(PermissionErrors.IdRequired.Message);

            RuleFor(x => x.Code)
                .NotEmpty().WithMessage(PermissionErrors.PermissionCodeRequired.Message)
                .Matches(Permission.PermissionCodeRegex).WithMessage(PermissionErrors.InvalidPermissionFormat.Message);

            RuleFor(x => x.Description)
                .MaximumLength(500);
        }
    }
}
