using FluentValidation;

namespace ControlHub.Application.Users.Commands.UpdateUsername
{
    public class UpdateUsernameCommandValidator : AbstractValidator<UpdateUsernameCommand>
    {
        public UpdateUsernameCommandValidator()
        {
        }
    }
}
