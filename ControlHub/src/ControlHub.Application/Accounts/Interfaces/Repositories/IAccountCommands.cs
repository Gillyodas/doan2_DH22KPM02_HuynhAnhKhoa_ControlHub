using ControlHub.Domain.Accounts;
using ControlHub.SharedKernel.Results;

namespace ControlHub.Application.Accounts.Interfaces.Repositories
{
    public interface IAccountCommands
    {
        public Task AddAsync(Account accDomain);
    }
}
