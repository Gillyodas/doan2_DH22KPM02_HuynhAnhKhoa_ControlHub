using ControlHub.Application.Permissions.DTOs;
using FluentValidation;

namespace ControlHub.Application.Permissions.Commands.CreatePermissions
{
    public class CreatePermissionDtoValidator : AbstractValidator<CreatePermissionDto>
    {
        public CreatePermissionDtoValidator()
        {
            RuleFor(p => p.Code)
                .NotEmpty().WithMessage("Permission code is required.")
                .MaximumLength(100).WithMessage("Permission code must not exceed 100 characters.");

            RuleFor(p => p.Description)
                .MaximumLength(255).WithMessage("Description must not exceed 255 characters.");
        }
    }
}
