using ControlHub.Application.Roles.DTOs;
using FluentValidation;

namespace ControlHub.Application.Roles.Commands.CreateRoles
{
    public class CreateRoleDtoValidator : AbstractValidator<CreateRoleDto>
    {
        public CreateRoleDtoValidator()
        {
            RuleFor(r => r.Name)
                .NotEmpty().WithMessage("Role name is required.")
                .MaximumLength(100).WithMessage("Role name must not exceed 100 characters.");

            RuleFor(r => r.Description)
                .NotEmpty().WithMessage("Role description is required.")
                .MaximumLength(255).WithMessage("Description must not exceed 255 characters.");

            RuleFor(r => r.PermissionIds)
                .Must(p => p == null || p.Distinct().Count() == p.Count())
                .WithMessage("Permission list contains duplicate values.");
        }
    }
}
