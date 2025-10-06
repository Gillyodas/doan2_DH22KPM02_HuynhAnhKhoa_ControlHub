using ControlHub.Application.Accounts.Interfaces.Repositories;
using ControlHub.Domain.Accounts;
using ControlHub.Domain.Accounts.Enums;
using ControlHub.Domain.Accounts.ValueObjects;
using ControlHub.Domain.Users;
using ControlHub.Infrastructure.Persistence;
using ControlHub.Infrastructure.Users;
using Microsoft.EntityFrameworkCore;

namespace ControlHub.Infrastructure.Accounts.Repositories
{
    public class AccountQueries : IAccountQueries
    {
        private readonly AppDbContext _db;

        public AccountQueries(AppDbContext db)
        {
            _db = db;
        }

        public async Task<Account?> GetWithoutUserByIdAsync(Guid id, CancellationToken cancellationToken)
        {
            var acc = await _db.Accounts
                .AsNoTracking()
                .Include(a => a.Identifiers)
                .FirstOrDefaultAsync(a => a.Id == id, cancellationToken);

            return acc is not null ? AccountMapper.ToDomain(acc) : null;
        }

        public async Task<Account?> GetByIdentifierAsync(IdentifierType identifierType, string normalizedValue, CancellationToken cancellationToken)
        {
            var entity = await _db.AccountIdentifiers
                .Where(i => i.Type == identifierType && i.NormalizedValue == normalizedValue)
                .Include(i => i.Account)
                    .ThenInclude(a => a.User)
                .SingleOrDefaultAsync(cancellationToken);

            return entity?.Account is null ? null : AccountMapper.ToDomain(entity.Account);
        }

        public async Task<Account?> GetByIdentifierWithoutUserAsync(IdentifierType identifierType, string normalizedValue, CancellationToken cancellationToken)
        {
            var entity = await _db.AccountIdentifiers
                .Where(i => i.Type == identifierType && i.NormalizedValue == normalizedValue)
                .Include(i => i.Account)
                .SingleOrDefaultAsync(cancellationToken);

            return entity?.Account is null ? null : AccountMapper.ToDomain(entity.Account);
        }

        public async Task<Identifier?> GetIdentifierByIdentifierAsync(IdentifierType identifierType, string normalizedValue, CancellationToken cancellationToken)
        {
            var ident = await _db.AccountIdentifiers
                .Where(i => i.Type == identifierType && i.NormalizedValue == normalizedValue)
                .SingleOrDefaultAsync(cancellationToken);

            return ident is null ? null : IdentifierMapper.ToDomain(ident);
        }

        public async Task<User?> GetUserById(Guid id, CancellationToken cancellationToken)
        {
            var user = await _db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.AccId == id, cancellationToken);

            return user is not null ? UserMapper.ToDomain(user) : null;
        }
    }
}
