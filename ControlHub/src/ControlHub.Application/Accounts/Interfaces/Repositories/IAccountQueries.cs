using ControlHub.Domain.Accounts;
using ControlHub.Domain.Accounts.Enums;
using ControlHub.Domain.Accounts.ValueObjects;
using ControlHub.Domain.Users;

namespace ControlHub.Application.Accounts.Interfaces.Repositories
{
    public interface IAccountQueries
    {
        Task<User?> GetUserById(
            Guid id,
            CancellationToken cancellationToken);
        Task<Account?> GetWithoutUserByIdAsync(
            Guid id,
            CancellationToken cancellationToken);
        Task<Account?> GetByIdentifierAsync(
            IdentifierType identifierType,
            string normalizedValue,
            CancellationToken cancellationToken);
        Task<Account?> GetByIdentifierWithoutUserAsync(
            IdentifierType identifierType,
            string normalizedValue,
            CancellationToken cancellationToken);

        Task<Identifier?> GetIdentifierByIdentifierAsync(
            IdentifierType identifierType,
            string normalizedValue,
            CancellationToken cancellationToken);
    }
}
