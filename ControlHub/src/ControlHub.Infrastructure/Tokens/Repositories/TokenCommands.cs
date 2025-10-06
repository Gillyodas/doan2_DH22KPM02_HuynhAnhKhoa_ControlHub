using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ControlHub.Application.Tokens.Interfaces.Repositories;
using ControlHub.Domain.Tokens;
using ControlHub.Infrastructure.Persistence;

namespace ControlHub.Infrastructure.Tokens.Repositories
{
    public class TokenCommands : ITokenCommands
    {
        private readonly AppDbContext _db;

        public TokenCommands(AppDbContext db)
        {
            _db = db;
        }

        public async Task AddAsync(Token domainToken, CancellationToken cancellationToken)
        {
            var entity = TokenMapper.ToEntity(domainToken);
            await _db.Tokens.AddAsync(entity, cancellationToken);
        }

        public async Task UpdateAsync(Token domainToken, CancellationToken cancellationToken)
        {
            var entity = TokenMapper.ToEntity(domainToken);
            _db.Tokens.Update(entity);
        }
    }
}
