using ControlHub.SharedKernel.Identity.Accounts;
using ControlHub.SharedKernel.TokenManagement;
using FluentValidation;

namespace ControlHub.Application.Identity.Commands.ResetPassword
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
