using ControlHub.Domain.Accounts;
using ControlHub.Domain.Accounts.ValueObjects;
using ControlHub.Domain.Users;
using ControlHub.SharedKernel.Results;

namespace ControlHub.Application.Accounts.Interfaces.Repositories
{
    public interface IAccountQueries
    {
        Task<Email?> GetEmailByEmailAsync(Email email, CancellationToken cancellationToken);
        Task<Account?> GetAccountByEmail(Email email, CancellationToken cancellationToken);
        Task<User?> GetUserById(Guid id, CancellationToken cancellationToken);
        Task<Account?> GetAccountWithoutUserById(Guid id, CancellationToken cancellationToken);
    }
}
