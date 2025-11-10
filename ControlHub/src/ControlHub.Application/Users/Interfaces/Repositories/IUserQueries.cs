using ControlHub.Domain.Users;

namespace ControlHub.Application.Users.Interfaces.Repositories
{
    public interface IUserQueries
    {
        public Task<User> GetByAccountId(Guid id, CancellationToken cancellationToken);
    }
}
