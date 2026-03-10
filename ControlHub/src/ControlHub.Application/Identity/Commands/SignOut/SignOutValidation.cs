using ControlHub.SharedKernel.TokenManagement;
using FluentValidation;

namespace ControlHub.Application.Identity.Commands.SignOut
{
    public class SignOutCommandValidator : AbstractValidator<SignOutCommand>
    {
        public SignOutCommandValidator()
        {
            RuleFor(x => x.accessToken)
                .NotEmpty().WithMessage(TokenErrors.TokenRequired.Message);

            RuleFor(x => x.refreshToken)
                .NotEmpty().WithMessage(TokenErrors.TokenRequired.Message);
        }
    }
}
