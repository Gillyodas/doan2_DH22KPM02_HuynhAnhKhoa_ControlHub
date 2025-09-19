using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ControlHub.SharedKernel.Accounts;
using FluentValidation;
using FluentValidation.Validators;

namespace ControlHub.Application.Accounts.Commands.ChangePassword
{
    public class ChangePasswordCommandValidator : AbstractValidator<ChangePasswordCommand>
    {
        public ChangePasswordCommandValidator()
        {
            // current password chỉ cần không rỗng
            RuleFor(x => x.curPassword)
                .NotEmpty().WithMessage(AccountErrors.PasswordRequired.Message);

            // new password cần đáp ứng policy
            RuleFor(x => x.newPassword)
                .NotEmpty().WithMessage(AccountErrors.PasswordRequired.Message)
                .MinimumLength(8).WithMessage(AccountErrors.PasswordTooShort.Message)
                .Matches("[A-Z]").WithMessage(AccountErrors.PasswordMissingUppercase.Message)
                .Matches("[a-z]").WithMessage(AccountErrors.PasswordMissingLowercase.Message)
                .Matches("[0-9]").WithMessage(AccountErrors.PasswordMissingDigit.Message)
                .Matches("[!@#$%^&*()]").WithMessage(AccountErrors.PasswordMissingSpecial.Message)
                .NotEqual(x => x.curPassword).WithMessage(AccountErrors.PasswordSameAsOld.Message);
        }
    }
}
