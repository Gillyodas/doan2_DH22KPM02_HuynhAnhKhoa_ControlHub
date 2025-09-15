using ControlHub.Application.Users.Interfaces.Repositories;
using ControlHub.Domain.Users;
using ControlHub.Infrastructure.Persistence;
using ControlHub.SharedKernel.Results;
using Microsoft.EntityFrameworkCore;

namespace ControlHub.Infrastructure.Users.Repositories
{
    public class UserCommands : IUserCommands
    {
        private readonly AppDbContext _db;

        public UserCommands(AppDbContext db)
        {
            _db = db;
        }

        public async Task AddAsync(User user, CancellationToken cancellationToken)
        {
            var userEntity = UserMapper.ToEntity(user);
            await _db.Users.AddAsync(userEntity);
            await _db.SaveChangesAsync();
        }

        public async Task SaveAsync(User user, CancellationToken cancellationToken)
        {
            var userEntity = UserMapper.ToEntity(user);
            _db.Users.Update(userEntity);
            await _db.SaveChangesAsync();
        }
    }
}
