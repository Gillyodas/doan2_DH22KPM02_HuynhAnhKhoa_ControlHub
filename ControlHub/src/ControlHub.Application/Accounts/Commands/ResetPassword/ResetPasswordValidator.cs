using ControlHub.SharedKernel.Accounts;
using ControlHub.SharedKernel.Tokens;
using FluentValidation;

namespace ControlHub.Application.Accounts.Commands.ResetPassword
{
    public class ResetPasswordValidator : AbstractValidator<ResetPasswordCommand>
    {
        public ResetPasswordValidator()
        {
            RuleFor(x => x.Token)
                .NotEmpty().WithMessage(TokenErrors.TokenRequired.Message);

            RuleFor(x => x.Password)
                .NotEmpty().WithMessage(AccountErrors.PasswordRequired.Message);
        }
    }
}
