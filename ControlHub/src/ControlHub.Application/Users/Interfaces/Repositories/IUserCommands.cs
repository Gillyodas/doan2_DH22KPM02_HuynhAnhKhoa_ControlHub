using ControlHub.Domain.Users;
using ControlHub.SharedKernel.Results;

namespace ControlHub.Application.Users.Interfaces.Repositories
{
    public interface IUserCommands
    {
        Task<Result<bool>> AddAsync(User user);
    }
}
