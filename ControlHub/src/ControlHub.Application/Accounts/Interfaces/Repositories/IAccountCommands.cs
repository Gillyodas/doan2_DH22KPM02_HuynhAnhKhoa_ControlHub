using ControlHub.Domain.Accounts;
using ControlHub.SharedKernel.Results;

namespace ControlHub.Application.Accounts.Interfaces.Repositories
{
    public interface IAccountCommands
    {
        public Task<Result<bool>> AddAsync(Account accDomain);
    }
}
