using ControlHub.Application.Accounts.Interfaces.Repositories;
using ControlHub.Domain.Accounts;
using ControlHub.SharedKernel.Results;
using ControlHub.Infrastructure.Persistence.Mappers;
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
        public async Task AddAsync(Account accDomain)
        {
            var accEntity = AccountMapper.ToEntity(accDomain);
            await _db.Accounts.AddAsync(accEntity);
            await _db.SaveChangesAsync();
        }
    }
}
