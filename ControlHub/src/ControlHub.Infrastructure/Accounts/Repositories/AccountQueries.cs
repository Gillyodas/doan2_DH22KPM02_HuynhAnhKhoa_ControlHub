using ControlHub.Application.Accounts.Interfaces.Repositories;
using ControlHub.Domain.Accounts.ValueObjects;
using ControlHub.Infrastructure.Persistence;
using ControlHub.SharedKernel.Results;
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
