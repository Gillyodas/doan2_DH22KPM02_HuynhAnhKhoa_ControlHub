using FluentValidation;

namespace ControlHub.Application.Permissions.Commands.CreatePermissions
{
    public class CreatePermissionsCommandValidator : AbstractValidator<CreatePermissionsCommand>
    {
        public CreatePermissionsCommandValidator()
        {
            RuleFor(x => x.Permissions)
                .NotNull().WithMessage("Permissions list is required.")
                .Must(p => p.Any()).WithMessage("At least one permission must be provided.");

            RuleForEach(x => x.Permissions)
                .SetValidator(new CreatePermissionDtoValidator());
        }
    }
}