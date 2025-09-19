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

            return Account.Rehydrate(
                entity.Id,
                entity.Email,
                password,
                entity.IsActive,
                entity.IsDeleted,
                user
            );
        }

        public static AccountEntity ToEntity(Account domain)
        {
            return new AccountEntity
            {
                Id = domain.Id,
                Email = domain.Email,
                HashPassword = domain.Password.Hash,
                Salt = domain.Password.Salt,
                IsActive = domain.IsActive,
                IsDeleted = domain.IsDeleted,
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
