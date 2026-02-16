using ControlHub.Domain.Identity.Aggregates;
using ControlHub.Domain.Identity.Enums;
using ControlHub.Domain.Identity.ValueObjects;
using ControlHub.Domain.Identity.Entities;

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
        Task<Account?> GetByIdentifierNameAsync(
            string identifierName,
            string normalizedValue,
            CancellationToken cancellationToken);
        Task<Guid> GetRoleIdByAccIdAsync(
            Guid accId,
            CancellationToken cancellationToken);
    }
}
