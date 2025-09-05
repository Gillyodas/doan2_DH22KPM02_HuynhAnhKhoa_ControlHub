using ControlHub.SharedKernel.Accounts;
using FluentValidation;

namespace ControlHub.Application.Accounts.Commands.CreateAccount
{
    public class CreateAccountCommandValidator : AbstractValidator<CreateAccountCommand>
    {
        public CreateAccountCommandValidator()
        {
            RuleFor(x => x.Email)
                .NotEmpty().WithMessage(AccountErrors.EmailRequired.Message)
                .EmailAddress().WithMessage(AccountErrors.InvalidEmail.Message);

            RuleFor(x => x.Password)
                .NotEmpty().WithMessage(AccountErrors.PasswordRequired.Message)
                .MinimumLength(8).WithMessage(AccountErrors.PasswordTooShort.Message)
                .Matches("[A-Z]").WithMessage(AccountErrors.PasswordMissingUppercase.Message)
                .Matches("[a-z]").WithMessage(AccountErrors.PasswordMissingLowercase.Message)
                .Matches("[0-9]").WithMessage(AccountErrors.PasswordMissingDigit.Message)
                .Matches("[!@#$%^&*()]").WithMessage(AccountErrors.PasswordMissingSpecial.Message);
        }
    }
}