using ControlHub.SharedKernel.Accounts;
using FluentValidation;

namespace ControlHub.Application.Accounts.Commands.ForgotPassword
{
    public class ForgotPasswordCommandValidator : AbstractValidator<ForgotPasswordCommand>
    {
        public ForgotPasswordCommandValidator()
        {
            RuleFor(x => x.Value)
                .NotEmpty().WithMessage(AccountErrors.IdentifierRequired.Message)
                .MaximumLength(300).WithMessage(AccountErrors.IdentifierTooLong.Message);
        }
    }
}
