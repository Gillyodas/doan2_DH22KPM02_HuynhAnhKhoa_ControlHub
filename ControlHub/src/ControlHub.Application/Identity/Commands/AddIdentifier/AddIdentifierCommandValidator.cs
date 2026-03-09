using FluentValidation;

namespace ControlHub.Application.Identity.Commands.AddIdentifier
{
    public class AddIdentifierCommandValidator : AbstractValidator<AddIdentifierCommand>
    {
        public AddIdentifierCommandValidator()
        {
        }
    }
}
