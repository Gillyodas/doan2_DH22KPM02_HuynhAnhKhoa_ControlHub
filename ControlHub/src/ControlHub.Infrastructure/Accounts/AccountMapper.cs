using ControlHub.Domain.Accounts;
using ControlHub.Domain.Accounts.ValueObjects;
using ControlHub.Domain.Users;
using ControlHub.Infrastructure.Users;
using ControlHub.SharedKernel.Results;

namespace ControlHub.Infrastructure.Accounts
{
    public static class AccountMapper
    {
        public static Account ToDomain(AccountEntity entity)
        {
            var user = entity.User != null
                ? Maybe<User>.From(new User(entity.User.Id, entity.User.AccId, entity.User.Username))
                : Maybe<User>.None;

            var password = Password.From(entity.HashPassword, entity.Salt);

            var identifiers = entity.Identifiers
                .Select(IdentifierMapper.ToDomain)
                .ToList();

            return Account.Rehydrate(
                entity.Id,
                password,
                entity.IsActive,
                entity.IsDeleted,
                user,
                identifiers
            );
        }

        public static AccountEntity ToEntity(Account domain)
        {
            return new AccountEntity
            {
                Id = domain.Id,
                HashPassword = domain.Password.Hash,
                Salt = domain.Password.Salt,
                IsActive = domain.IsActive,
                IsDeleted = domain.IsDeleted,
                Identifiers = domain.Identifiers
                    .Select(i => IdentifierMapper.ToEntity(i, domain.Id))
                    .ToList(),
                User = domain.User.Match(
                    some: u => new UserEntity
                    {
                        Id = u.Id,
                        AccId = u.AccId,
                        Username = u.Username
                    },
                    none: () => null!
                )
            };
        }
    }
}
