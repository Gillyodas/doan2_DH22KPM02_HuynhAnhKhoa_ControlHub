using ControlHub.Application.Tokens.Interfaces.Repositories;
using ControlHub.Domain.TokenManagement.Aggregates;
using ControlHub.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace ControlHub.Infrastructure.TokenManagement.Persistence.Repositories
{
    internal class TokenQueries : ITokenQueries
    {
        private readonly AppDbContext _db;

        public TokenQueries(AppDbContext db)
        {
            _db = db;
        }
        public async Task<Token?> GetByValueAsync(string Value, CancellationToken cancellationToken)
        {
            return await _db.Tokens
                .AsNoTracking()
                .Where(t => t.Value == Value)
                .FirstOrDefaultAsync(cancellationToken);
        }
    }
}
