using ControlHub.Application.Tokens.Interfaces.Repositories;
using ControlHub.Domain.Tokens;
using ControlHub.Infrastructure.Persistence;
using ControlHub.SharedKernel.Common.Exceptions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace ControlHub.Infrastructure.Tokens.Repositories
{
    internal class TokenRepository : ITokenRepository
    {
        private readonly AppDbContext _db;
        private readonly ILogger<TokenRepository> _logger;

        public TokenRepository(AppDbContext db, ILogger<TokenRepository> logger)
        {
            _db = db;
            _logger = logger;
        }

        public async Task AddAsync(Token token, CancellationToken cancellationToken)
        {
            try
            {
                await _db.Tokens.AddAsync(token, cancellationToken);
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(ex, "Failed to add Token {Value}", token.Value);
                throw new RepositoryException("Error adding token to database.", ex);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error while adding Token {Value}", token.Value);
                throw new RepositoryException("Unexpected error while adding token.", ex);
            }
        }

        public async Task<Token?> GetByIdAsync(Guid tokenId, CancellationToken cancellationToken)
        {
            return await _db.Tokens.Where(t => t.Id == tokenId).SingleOrDefaultAsync(cancellationToken);
        }

        public async Task<IEnumerable<Token>> GetTokensByAccountIdAsync(Guid accId, CancellationToken cancellationToken)
        {
            return await _db.Tokens.Where(t => t.AccountId == accId).ToListAsync(cancellationToken);
        }
    }
}