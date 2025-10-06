using ControlHub.Application.Accounts.Interfaces;
using ControlHub.Application.Accounts.Interfaces.Repositories;
using ControlHub.Domain.Accounts.Enums;
using ControlHub.Domain.Accounts.ValueObjects;
using ControlHub.Domain.Users;
using ControlHub.SharedKernel.Results;

namespace ControlHub.Infrastructure.Accounts.Validators
{
    public class AccountValidator : IAccountValidator
    {
        private readonly IAccountQueries _accountQueries;

        public AccountValidator(IAccountQueries accountQueries)
        {
            _accountQueries = accountQueries;
        }

        public async Task<bool> IdentifierIsExist(string Value, IdentifierType Type, CancellationToken cancellationToken)
        {
            return await _accountQueries.GetIdentifierByIdentifierAsync(Type, Value, cancellationToken) != null ? true : false;
        }
    }
}
