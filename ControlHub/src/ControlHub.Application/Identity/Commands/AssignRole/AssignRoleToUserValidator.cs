using FluentValidation;

namespace ControlHub.Application.Identity.Commands.AssignRole
{
    public class AssignRoleToUserValidator : AbstractValidator<AssignRoleToUserCommand>
    {
        public AssignRoleToUserValidator()
        {
            RuleFor(x => x.UserId)
                .NotEmpty().WithMessage("User Id is required.");

            RuleFor(x => x.RoleId)
                .NotEmpty().WithMessage("Role Id is required.");
        }
    }
}
