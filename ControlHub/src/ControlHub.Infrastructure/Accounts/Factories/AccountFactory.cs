using ControlHub.Application.Accounts.Interfaces;
using ControlHub.Domain.Identity.Aggregates;
using ControlHub.Domain.Identity.Enums;
using ControlHub.Domain.Identity.Identifiers.Services;
using ControlHub.Domain.Identity.Security;
using ControlHub.Domain.Identity.Entities;
using ControlHub.SharedKernel.Results;

namespace ControlHub.Infrastructure.Accounts.Factories
{
    internal class AccountFactory : IAccountFactory
    {
        private readonly IPasswordHasher _passwordHasher;
        private readonly IdentifierFactory _identifierFactory;

        public AccountFactory(
            IPasswordHasher passwordHasher,
            IdentifierFactory identifierFactory)
        {
            _passwordHasher = passwordHasher;
            _identifierFactory = identifierFactory;
        }

        public async Task<Result<Maybe<Account>>> CreateWithUserAndIdentifierAsync(
            Guid accountId,
            string identifierValue,
            IdentifierType identifierType,
            string rawPassword,
            Guid roleId,
            string? username = "No name",
            Guid? identifierConfigId = null)
        {
            var pass = _passwordHasher.Hash(rawPassword);

            var account = Account.Create(accountId, pass, roleId);

            var result = await _identifierFactory.CreateAsync(identifierType, identifierValue, identifierConfigId);

            if (result.IsFailure)
                return Result<Maybe<Account>>.Failure(result.Error);

            var addResult = account.AddIdentifier(result.Value);
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
