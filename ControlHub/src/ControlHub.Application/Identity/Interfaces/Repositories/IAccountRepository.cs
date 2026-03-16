using ControlHub.Domain.Identity.Aggregates;
namespace ControlHub.Application.Identity.Interfaces.Repositories
{
    public interface IAccountRepository
    {
        Task AddAsync(Account acc, CancellationToken cancellationToken);
        Task<Account?> GetWithoutUserByIdAsync(
            Guid id,
            CancellationToken cancellationToken);
        Task<Account?> GetByIdentifierWithoutUserAsync(
            string normalizedValue,
            CancellationToken cancellationToken);

        Task<List<Account>> GetByRoleIdAsync(Guid roleId, CancellationToken cancellationToken);
    }
}
