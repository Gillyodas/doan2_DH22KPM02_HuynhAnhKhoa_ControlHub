using ControlHub.Application.Accounts.Interfaces;
using ControlHub.Application.Accounts.Interfaces.Repositories;
using ControlHub.Domain.Identity.Enums;

namespace ControlHub.Infrastructure.Identity.Persistence.Repositories
{
    internal class AccountValidator : IAccountValidator
    {
        private readonly IAccountQueries _accountQueries;

        public AccountValidator(IAccountQueries accountQueries)
        {
            _accountQueries = accountQueries;
        }

        public async Task<bool> IdentifierIsExist(string Value, CancellationToken cancellationToken)
        {
            return await _accountQueries.GetIdentifierByIdentifierAsync(Value, cancellationToken) != null ? true : false;
        }
    }
}
