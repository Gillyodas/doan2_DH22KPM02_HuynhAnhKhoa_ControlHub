using ControlHub.Domain.TokenManagement.Aggregates;

namespace ControlHub.Application.TokenManagement.Interfaces.Repositories
{
    public interface ITokenRepository
    {
        Task AddAsync(Token token, CancellationToken cancellationToken);
        Task<Token?> GetByIdAsync(Guid tokenId, CancellationToken cancellationToken);
        Task<IEnumerable<Token>> GetTokensByAccountIdAsync(Guid accId, CancellationToken cancellationToken);
    }
}
