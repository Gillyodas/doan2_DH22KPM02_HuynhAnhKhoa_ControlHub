using ControlHub.Domain.Accounts;
using ControlHub.Domain.Accounts.ValueObjects;
using ControlHub.Domain.Users;
using ControlHub.SharedKernel.Results;

namespace ControlHub.Domain.Common.Factories
{
    public class AccountFactory
    {
        public static Result<Maybe<Account>> CreateWithUser(Guid accountId, Email email, byte[] hash, byte[] salt, string? username = "No name")
        {
            var account = Account.Create(accountId, email, hash, salt);

            var user = new User(Guid.NewGuid(), accountId, username);

            var attachResult = account.AttachUser(user);
            if (!attachResult.IsSuccess)
                return Result<Maybe<Account>>.Failure(attachResult.Error);

            return Result<Maybe<Account>>.Success(Maybe<Account>.From(account));
        }
    }
}
