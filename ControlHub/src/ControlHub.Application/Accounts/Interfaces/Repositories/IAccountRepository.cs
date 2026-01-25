using ControlHub.Domain.Accounts;
using ControlHub.Domain.Accounts.Enums;
namespace ControlHub.Application.Accounts.Interfaces.Repositories
{
    public interface IAccountRepository
    {
        Task AddAsync(Account acc, CancellationToken cancellationToken);
        Task<Account?> GetWithoutUserByIdAsync(
            Guid id,
            CancellationToken cancellationToken);
        Task<Account?> GetByIdentifierWithoutUserAsync(
            IdentifierType identifierType,
            string normalizedValue,
            CancellationToken cancellationToken);

        Task<List<Account>> GetByRoleIdAsync(Guid roleId, CancellationToken cancellationToken);
    }
}
