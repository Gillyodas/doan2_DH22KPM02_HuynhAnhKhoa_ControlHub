using ControlHub.Domain.Tokens;

namespace ControlHub.Application.Tokens.Interfaces.Repositories
{
    public interface ITokenRepository
    {
        Task AddAsync(Token token, CancellationToken cancellationToken);
        Task<Token?> GetByIdAsync(Guid tokenId, CancellationToken cancellationToken);
        Task<IEnumerable<Token>> GetTokensByAccountIdAsync(Guid accId, CancellationToken cancellationToken);
    }
}
