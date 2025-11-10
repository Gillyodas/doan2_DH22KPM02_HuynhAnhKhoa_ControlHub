using ControlHub.Domain.Accounts.Enums;
using ControlHub.Domain.Accounts.Identifiers.Interfaces;
using ControlHub.Domain.Accounts.Interfaces.Security;
using ControlHub.Domain.Accounts.ValueObjects;
using ControlHub.Domain.Users;
using ControlHub.SharedKernel.Accounts;
using ControlHub.SharedKernel.Results;

namespace ControlHub.Domain.Accounts.Services
{
    public class RegisterService
    {
        public static Result<Maybe<Account>> CreateWithUserAndIdentifier(
            Guid accountId,
            string Value,
            IdentifierType Type,
            string Pass,
            IPasswordHasher passwordHasher,
            IIdentifierValidatorFactory identifierValidatorFactory,
            string? username = "No name")
        {
            var pass = passwordHasher.Hash(Pass);

            var account = Account.Create(accountId, pass);

            var validator = identifierValidatorFactory.Get(Type);
            if (validator == null)
                return Result<Maybe<Account>>.Failure(AccountErrors.UnsupportedIdentifierType);

            var (isValid, normalized, error) = validator.ValidateAndNormalize(Value);

            if (!isValid)
                return Result<Maybe<Account>>.Failure(error);

            var ident = Identifier.Create(Type, Value, normalized);

            var addResult = account.AddIdentifier(ident);
            if (!addResult.IsSuccess)
                return Result<Maybe<Account>>.Failure(addResult.Error);

            var user = new User(Guid.NewGuid(), accountId, username);

            var attachResult = account.AttachUser(user);
            if (!attachResult.IsSuccess)
                return Result<Maybe<Account>>.Failure(attachResult.Error);

            return Result<Maybe<Account>>.Success(Maybe<Account>.From(account));
        }
    }
}