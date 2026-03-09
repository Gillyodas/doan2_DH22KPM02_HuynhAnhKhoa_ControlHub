using ControlHub.Domain.Identity.Aggregates;
using ControlHub.Domain.Identity.Entities;
using ControlHub.Domain.Identity.Enums;
using ControlHub.Domain.Identity.ValueObjects;

namespace ControlHub.Application.Identity.Interfaces.Repositories
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
            string normalizedValue,
            CancellationToken cancellationToken);
        Task<Account?> GetByIdentifierWithoutUserAsync(
            string normalizedValue,
            CancellationToken cancellationToken);
        Task<Identifier?> GetIdentifierByIdentifierAsync(
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
