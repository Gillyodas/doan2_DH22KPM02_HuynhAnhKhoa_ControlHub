using ControlHub.Application.Accounts.Interfaces.Repositories;
using ControlHub.Domain.Accounts;
using ControlHub.SharedKernel.Results;
using ControlHub.Infrastructure.Persistence;

namespace ControlHub.Infrastructure.Accounts.Repositories
{
    public class AccountCommands : IAccountCommands
    {
        private readonly AppDbContext _db;

        public AccountCommands(AppDbContext db)
        {
            _db = db;
        }
        public async Task AddAsync(Account accDomain, CancellationToken cancellationToken)
        {
            var accEntity = AccountMapper.ToEntity(accDomain);
            await _db.Accounts.AddAsync(accEntity, cancellationToken);
            await _db.SaveChangesAsync(cancellationToken);
        }

        public async Task UpdateAsync(Account accDomain, CancellationToken cancellationToken)
        {
            var accEntity = AccountMapper.ToEntity(accDomain);
            _db.Accounts.Update(accEntity);
            await _db.SaveChangesAsync(cancellationToken);
        }
    }
}
