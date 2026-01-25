using ControlHub.Application.Accounts.Interfaces.Repositories;
using ControlHub.Domain.Accounts;
using ControlHub.Domain.Accounts.Enums;
using ControlHub.Infrastructure.Persistence;
using ControlHub.SharedKernel.Common.Exceptions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace ControlHub.Infrastructure.Accounts.Repositories
{
    internal class AccountRepository : IAccountRepository
    {
        private readonly AppDbContext _db;
        private readonly ILogger<AccountRepository> _logger;

        public AccountRepository(AppDbContext db, ILogger<AccountRepository> logger)
        {
            _db = db;
            _logger = logger;
        }

        public async Task AddAsync(Account acc, CancellationToken cancellationToken)
        {
            try
            {
                await _db.Accounts.AddAsync(acc, cancellationToken);
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(ex, "Failed to add Account {Id}", acc.Id);
                throw new RepositoryException("Error adding account to database.", ex);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error while adding Account {Id}", acc.Id);
                throw new RepositoryException("Unexpected error while adding account.", ex);
            }
        }

        public async Task<Account?> GetWithoutUserByIdAsync(Guid id, CancellationToken cancellationToken)
        {
            return await _db.Accounts
                .FirstOrDefaultAsync(a => a.Id == id, cancellationToken);
        }

        public async Task<Account?> GetByIdentifierWithoutUserAsync(IdentifierType identifierType, string normalizedValue, CancellationToken cancellationToken)
        {
            return await _db.Accounts
                .Where(a => a.Identifiers.Any(i => i.Type == identifierType && i.NormalizedValue == normalizedValue))
                .FirstOrDefaultAsync(cancellationToken);
        }

        public async Task<List<Account>> GetByRoleIdAsync(Guid roleId, CancellationToken cancellationToken)
        {
            return await _db.Accounts
                .Include(a => a.User)
                .Include(a => a.Role)
                .Where(a => a.RoleId == roleId)
                .ToListAsync(cancellationToken);
        }
    }
}