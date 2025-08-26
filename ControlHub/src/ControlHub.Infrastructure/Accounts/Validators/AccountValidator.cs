using ControlHub.Application.Accounts.Interfaces;
using ControlHub.Application.Accounts.Interfaces.Repositories;
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

        public async Task<Result<bool>> EmailIsExistAsync(Email email)
        {
            var result = await _accountQueries.GetByEmail(email);

            if (!result.IsSuccess)
                return Result<bool>.Failure(result.Error, result.Exception);

            return Result<bool>.Success(result.Value.HasValue);
        }

        public Result ValidatePassword(string password)
        {
            throw new NotImplementedException();
        }

        public Task<Result> ValidateUsernameAsync(string username)
        {
            throw new NotImplementedException();
        }
    }
}
