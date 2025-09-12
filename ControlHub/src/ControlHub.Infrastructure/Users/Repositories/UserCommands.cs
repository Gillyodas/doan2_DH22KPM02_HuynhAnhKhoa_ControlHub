using ControlHub.Application.Users.Interfaces.Repositories;
using ControlHub.Domain.Users;
using ControlHub.Infrastructure.Persistence;
using ControlHub.SharedKernel.Results;

namespace ControlHub.Infrastructure.Users.Repositories
{
    public class UserCommands : IUserCommands
    {
        private readonly AppDbContext _db;

        public UserCommands(AppDbContext db)
        {
            _db = db;
        }

        public async Task<Result<bool>> AddAsync(User user)
        {
            try
            {
                var userEntity = UserMapper.ToEntity(user);

                await _db.Users.AddAsync(userEntity);

                int rowAffected = await _db.SaveChangesAsync();

                return Result<bool>.Success(rowAffected > 0);
            }
            catch(Exception ex)
            {
                return Result<bool>.Failure("Db error", ex);
            }
        }
    }
}
