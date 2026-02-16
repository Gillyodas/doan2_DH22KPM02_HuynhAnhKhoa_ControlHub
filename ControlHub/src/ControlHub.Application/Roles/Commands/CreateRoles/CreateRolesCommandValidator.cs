using FluentValidation;

namespace ControlHub.Application.Roles.Commands.CreateRoles
{
    public class CreateRolesCommandValidator : AbstractValidator<CreateRolesCommand>
    {
        public CreateRolesCommandValidator()
        {
            // Ki?m tra danh sách Roles không null ho?c r?ng
            RuleFor(x => x.Roles)
                .NotNull().WithMessage("Roles list is required.")
                .Must(r => r.Any()).WithMessage("At least one role must be provided.");

            // Validate t?ng ph?n t? trong danh sách Roles
            RuleForEach(x => x.Roles)
                .SetValidator(new CreateRoleDtoValidator());
        }
    }
}
