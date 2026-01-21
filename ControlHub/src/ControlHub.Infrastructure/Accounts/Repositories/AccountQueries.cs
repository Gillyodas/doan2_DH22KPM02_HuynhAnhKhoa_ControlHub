using ControlHub.Application.Accounts.Interfaces.Repositories;
using ControlHub.Domain.Accounts;
using ControlHub.Domain.Accounts.Enums;
using ControlHub.Domain.Accounts.ValueObjects;
using ControlHub.Domain.Users;
using ControlHub.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace ControlHub.Infrastructure.Accounts.Repositories
{
    internal class AccountQueries : IAccountQueries
    {
        private readonly AppDbContext _db;

        public AccountQueries(AppDbContext db)
        {
            _db = db;
        }

        public async Task<Account?> GetWithoutUserByIdAsync(Guid id, CancellationToken cancellationToken)
        {
            return await _db.Accounts
                .AsNoTracking()
                // Owned Collection (Identifiers) thường được EF Core tự động load (Auto Include)
                // Nhưng explicit include cũng không sao nếu bạn tắt Auto Include
                // .Include(a => a.Identifiers) 
                .FirstOrDefaultAsync(a => a.Id == id, cancellationToken);
        }

        public async Task<Account?> GetByIdentifierAsync(IdentifierType identifierType, string normalizedValue, CancellationToken cancellationToken)
        {
            return await _db.Accounts
                .AsNoTracking()
                .Include(a => a.User)
                .Where(a => a.Identifiers.Any(i => i.Type == identifierType && i.NormalizedValue == normalizedValue))
                .FirstOrDefaultAsync(cancellationToken);
        }

        public async Task<Account?> GetByIdentifierWithoutUserAsync(IdentifierType identifierType, string normalizedValue, CancellationToken cancellationToken)
        {
            return await _db.Accounts
                .AsNoTracking()
                .Where(a => a.Identifiers.Any(i => i.Type == identifierType && i.NormalizedValue == normalizedValue))
                .FirstOrDefaultAsync(cancellationToken);
        }

        public async Task<Identifier?> GetIdentifierByIdentifierAsync(IdentifierType identifierType, string normalizedValue, CancellationToken cancellationToken)
        {
            return await _db.Accounts
                .AsNoTracking()
                .SelectMany(a => a.Identifiers)
                .Where(i => i.Type == identifierType && i.NormalizedValue == normalizedValue)
                .FirstOrDefaultAsync(cancellationToken);
        }

        public async Task<Account?> GetByIdentifierNameAsync(string identifierName, string normalizedValue, CancellationToken cancellationToken)
        {
            return await _db.Accounts
                .AsNoTracking()
                .Include(a => a.User)
                .Where(a => a.Identifiers.Any(i => i.Name == identifierName && i.NormalizedValue == normalizedValue))
                .FirstOrDefaultAsync(cancellationToken);
        }

        public async Task<User?> GetUserById(Guid id, CancellationToken cancellationToken)
        {
            return await _db.Users
                .AsNoTracking()
                .FirstOrDefaultAsync(u => u.AccId == id, cancellationToken);
        }

        public async Task<Guid> GetRoleIdByAccIdAsync(Guid accId, CancellationToken cancellationToken)
        {
            return await _db.Accounts
                .AsNoTracking()
                .Where(a => a.Id == accId)
                .Select(a => a.RoleId)
                .FirstOrDefaultAsync(cancellationToken);
        }
    }
}