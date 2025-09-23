using ControlHub.SharedKernel.Accounts;
using FluentValidation;

namespace ControlHub.Application.Accounts.Commands.ForgotPassword
{
    public class ForgotPasswordCommandValidator : AbstractValidator<ForgotPasswordCommand>
    {
        public ForgotPasswordCommandValidator()
        {
            RuleFor(x => x.email)
                .NotEmpty().WithMessage(AccountErrors.EmailRequired.Message)
                .EmailAddress().WithMessage(AccountErrors.InvalidEmail.Message);
        }
    }
}
