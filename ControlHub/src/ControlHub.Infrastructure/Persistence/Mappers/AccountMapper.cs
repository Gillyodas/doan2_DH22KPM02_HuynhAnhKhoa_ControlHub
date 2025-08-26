using ControlHub.Domain.Accounts;
using ControlHub.Domain.Users;
using ControlHub.Infrastructure.Persistence.Models;

namespace ControlHub.Infrastructure.Persistence.Mappers
{
    public static class AccountMapper
    {
        public static Account ToDomain(AccountEntity entity)
        {
            return Account.Rehydrate(
                entity.Id,
                entity.Email,
                entity.HashPassword,
                entity.Salt,
                entity.IsActive,
                entity.IsDeleted,
                entity.User != null
                    ? new User(entity.User.Id, entity.User.AccId, entity.User.Username)
                    : null
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
                User = domain.User != null
                    ? new UserEntity
                    {
                        Id = domain.User.Id,
                        AccId = domain.User.AccId,
                        Username = domain.User.Username
                    }
                    : null
            };
        }
    }
}
