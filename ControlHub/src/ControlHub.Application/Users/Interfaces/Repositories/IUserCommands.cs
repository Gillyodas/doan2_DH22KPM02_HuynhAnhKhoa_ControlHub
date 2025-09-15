using ControlHub.Domain.Users;
using ControlHub.SharedKernel.Results;

namespace ControlHub.Application.Users.Interfaces.Repositories
{
    public interface IUserCommands
    {
        Task AddAsync(User user, CancellationToken cancellationToken);
        Task SaveAsync(User user, CancellationToken cancellationToken);
    }
}
