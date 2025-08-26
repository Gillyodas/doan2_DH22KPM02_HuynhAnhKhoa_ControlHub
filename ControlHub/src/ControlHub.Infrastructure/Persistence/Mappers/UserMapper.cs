using ControlHub.Domain.Users;
using ControlHub.Infrastructure.Persistence.Models;

namespace ControlHub.Infrastructure.Persistence.Mappers
{
    public static class UserMapper
    {
        // Domain → Persistence
        public static UserEntity ToEntity(User domain) => new UserEntity
        {
            Id = domain.Id,
            Username = domain.Username,
            IsDeleted = domain.IsDeleted,
            AccId = domain.AccId
        };

        // Persistence → Domain
        public static User ToDomain(UserEntity entity) =>
            new User(entity.Id, entity.AccId, entity.Username);
    }
}
