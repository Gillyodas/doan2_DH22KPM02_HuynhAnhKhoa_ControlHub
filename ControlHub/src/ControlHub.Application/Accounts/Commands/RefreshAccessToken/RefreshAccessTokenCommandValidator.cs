using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ControlHub.SharedKernel.Accounts;
using ControlHub.SharedKernel.Tokens;
using FluentValidation;

namespace ControlHub.Application.Accounts.Commands.RefreshAccessToken
{
    public class RefreshAccessTokenCommandValidator : AbstractValidator<RefreshAccessTokenCommand>
    {
        public RefreshAccessTokenCommandValidator()
        {
            RuleFor(x => x.Value)
                .NotEmpty().WithMessage(TokenErrors.TokenRequired.Message);

            RuleFor(x => x.accId)
                .NotEmpty().WithMessage(AccountErrors.AccountIdRequired.Message);

            RuleFor(x => x.accessValue)
                .NotEmpty().WithMessage(AccountErrors.AccountIdRequired.Message);
        }
    }
}
