using ControlHub.Domain.Accounts;
using ControlHub.Domain.Users;
using ControlHub.SharedKernel.Results;
using ControlHub.Infrastructure.Users;

namespace ControlHub.Infrastructure.Accounts
{
    public static class AccountMapper
    {
        public static Account ToDomain(AccountEntity entity)
        {
            var user = entity.User != null
                ? Maybe<User>.From(new User(entity.User.Id, entity.User.AccId, entity.User.Username))
                : Maybe<User>.None;

            return Account.Rehydrate(
                entity.Id,
                entity.Email,
                entity.HashPassword,
                entity.Salt,
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
                HashPassword = domain.HashPassword,
                Salt = domain.Salt,
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
