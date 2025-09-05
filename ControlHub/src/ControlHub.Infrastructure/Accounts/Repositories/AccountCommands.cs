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
        public async Task<Result<bool>> AddAsync(Account accDomain)
        {
            try
            {
                var accEntity = AccountMapper.ToEntity(accDomain);

                await _db.Accounts.AddAsync(accEntity);
                int rowEffected = await _db.SaveChangesAsync();

                return Result<bool>.Success(rowEffected > 0);
            }
            catch(Exception ex)
            {
                return Result<bool>.Failure("Db error", ex);
            }
        }
    }
}
