using FluentValidation;

namespace ControlHub.Application.Identity.Commands.UpdateUsername
{
    public class UpdateUsernameCommandValidator : AbstractValidator<UpdateUsernameCommand>
    {
        public UpdateUsernameCommandValidator()
        {
        }
    }
}
