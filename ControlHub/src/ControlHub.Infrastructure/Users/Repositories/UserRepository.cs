using ControlHub.Application.Users.Interfaces.Repositories;
using ControlHub.Domain.Identity.Entities;
using ControlHub.Infrastructure.Persistence;
using ControlHub.SharedKernel.Common.Exceptions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace ControlHub.Infrastructure.Users.Repositories
{
    internal class UserRepository : IUserRepository
    {
        private readonly AppDbContext _db;
        private readonly ILogger<UserRepository> _logger;

        public UserRepository(AppDbContext db, ILogger<UserRepository> logger)
        {
            _db = db;
            _logger = logger;
        }

        public async Task AddAsync(User user, CancellationToken cancellationToken)
        {
            try
            {
                await _db.Users.AddAsync(user, cancellationToken);
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(ex, "Failed to add User {Id}", user.Id);
                throw new RepositoryException("Error adding user to database.", ex);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error while adding User {Id}", user.Id);
                throw new RepositoryException("Unexpected error while adding user.", ex);
            }
        }

        public async Task<User> GetByAccountId(Guid id, CancellationToken cancellationToken)
        {
            return await _db.Users.FirstOrDefaultAsync(u => u.AccId == id);
        }

        public async Task<User?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
        {
            return await _db.Users.FirstOrDefaultAsync(u => u.Id == id, cancellationToken);
        }

        public async Task SaveChangesAsync(CancellationToken cancellationToken)
        {
            try
            {
                await _db.SaveChangesAsync(cancellationToken);
            }
            catch (DbUpdateConcurrencyException ex)
            {
                _logger.LogError(ex, "Concurrency conflict during User SaveChanges");
                throw new RepositoryConcurrencyException("Concurrency conflict while saving users.", ex);
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(ex, "Database update error during User SaveChanges");
                throw new RepositoryException("Database update error while saving users.", ex);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error during User SaveChanges");
                throw new RepositoryException("Unexpected error during user save operation.", ex);
            }
        }
    }
}
