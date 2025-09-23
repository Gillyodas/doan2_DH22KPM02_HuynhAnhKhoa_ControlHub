using ControlHub.Application.Accounts.Interfaces.Repositories;
using ControlHub.Domain.Accounts;
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

        public async Task<Account?> GetAccountByEmail(Email email, CancellationToken cancellationToken)
        {
            var acc = await _db.Accounts
                               .AsNoTracking()
                               .Include(a => a.User)
                               .FirstOrDefaultAsync(a => a.Email == email, cancellationToken);

            return acc is not null ? AccountMapper.ToDomain(acc) : null;
        }

        public async Task<Account?> GetAccountWithoutUserById(Guid id, CancellationToken cancellationToken)
        {
            var acc = await _db.Accounts.AsNoTracking().FirstOrDefaultAsync(a => a.Id == id, cancellationToken);

            return acc is not null ? AccountMapper.ToDomain(acc) : null;
        }

        public async Task<Email?> GetEmailByEmailAsync(Email email, CancellationToken cancellationToken)
        {
            return await _db.Accounts
                .AsNoTracking()
                .Where(a => a.Email == email)
                .Select(a => a.Email)
                .FirstOrDefaultAsync(cancellationToken);
        }

        public async Task<User?> GetUserById(Guid id, CancellationToken cancellationToken)
        {
            var user = await _db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.AccId == id, cancellationToken);

            return user is not null ? UserMapper.ToDomain(user) : null;
        }
    }
}
