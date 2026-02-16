using FluentValidation;

namespace ControlHub.Application.Accounts.Commands.AddIdentifier
{
    public class AddIdentifierCommandValidator : AbstractValidator<AddIdentifierCommand>
    {
        public AddIdentifierCommandValidator()
        {
        }
    }
}
