using ControlHub.Application.Tokens.Interfaces.Repositories;
using ControlHub.Domain.Tokens;
using ControlHub.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace ControlHub.Infrastructure.Tokens.Repositories
{
    public class TokenQueries : ITokenQueries
    {
        private readonly AppDbContext _db;

        public TokenQueries(AppDbContext db)
        {
            _db = db;
        }
        public async Task<Token?> GetByValueAsync(string Value, CancellationToken cancellationToken)
        {
            var token = await _db.Tokens
                .AsNoTracking()
                .Where(t => t.Value == Value)
                .FirstOrDefaultAsync(cancellationToken);

            return token != null ? TokenMapper.ToDomain(token) : null;
        }
    }
}
