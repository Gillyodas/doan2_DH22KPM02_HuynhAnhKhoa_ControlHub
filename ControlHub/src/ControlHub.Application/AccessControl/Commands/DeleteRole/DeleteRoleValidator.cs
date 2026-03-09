using FluentValidation;

namespace ControlHub.Application.AccessControl.Commands.DeleteRole
{
    public class DeleteRoleValidator : AbstractValidator<DeleteRoleCommand>
    {
        public DeleteRoleValidator()
        {
            RuleFor(x => x.Id).NotEmpty().WithMessage("Role Id is required.");
        }
    }
}
