using ControlHub.Application.Accounts.Interfaces.Repositories;
using ControlHub.Domain.Accounts;
using ControlHub.Domain.Accounts.ValueObjects;
using ControlHub.Infrastructure.Persistence;
using ControlHub.SharedKernel.Results;
using ControlHub.SharedKernel.Accounts;
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

        public async Task<Result<Maybe<Account>>> GetAccountByEmail(Email email)
        {
            try
            {
                AccountEntity? resultAccount = await _db.Accounts
                                                  .AsNoTracking().Where(a => a.Email == email)
                                                  .FirstOrDefaultAsync();

                if (resultAccount == null)
                    return Result<Maybe<Account>>.Failure(AccountErrors.EmailNotFound.Code);

                Account domainAccount = AccountMapper.ToDomain(resultAccount);

                return Result<Maybe<Account>>.Success(Maybe<Account>.From(domainAccount));
            }
            catch(Exception ex)
            {
                return Result<Maybe<Account>>.Failure("Db error", ex);
            }
        }

        public async Task<Result<Maybe<Email>>> GetByEmail(Email email)
        {
            try
            {
                Email? resultEmail = await _db.Accounts
                                              .AsNoTracking()
                                              .Where(a => a.Email == email)
                                              .Select(a => a.Email)
                                              .FirstOrDefaultAsync();

                return Result<Maybe<Email>>.Success(Maybe<Email>.From(resultEmail));
            }
            catch (Exception ex)
            {
                return Result<Maybe<Email>>.Failure("Db error", ex);
            }
        }

    }
}
