using ControlHub.Application.Common.Persistence;
using ControlHub.SharedKernel.Common.Exceptions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Logging;

namespace ControlHub.Infrastructure.Persistence
{
    internal class UnitOfWork : IUnitOfWork
    {
        private readonly AppDbContext _dbContext;
        private readonly ILogger<UnitOfWork> _logger;
        private IDbContextTransaction? _currentTransaction;

        public UnitOfWork(AppDbContext dbContext, ILogger<UnitOfWork> logger)
        {
            _dbContext = dbContext;
            _logger = logger;
        }

        public bool HasActiveTransaction => _currentTransaction != null;

        public async Task<IDbContextTransaction> BeginTransactionAsync(CancellationToken ct = default)
        {
            if (_currentTransaction != null)
            {
                throw new InvalidOperationException("Transaction already started. Nested transactions not supported.");
            }

            _currentTransaction = await _dbContext.Database.BeginTransactionAsync(ct);

            _logger.LogInformation("Explicit transaction started");

            return _currentTransaction;
        }

        public async Task<int> CommitAsync(CancellationToken ct = default)
        {
            if (_currentTransaction != null)
            {
                return await SaveChangesAsync(ct);
            }

            await using var transaction = await _dbContext.Database.BeginTransactionAsync(ct);
            try
            {
                _logger.LogInformation("Implicit transaction started");

                var changes = await SaveChangesAsync(ct);
                await transaction.CommitAsync(ct);

                _logger.LogInformation(
                "Transaction committed successfully with {Changes} changes.",
                changes);

                return changes;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Transaction failed. Rolling back...");
                await SafeRollbackAsync(transaction, ct);
                _dbContext.ChangeTracker.Clear();
                throw MapException(ex);
            }
        }

        public async Task CommitTransactionAsync(CancellationToken ct = default)
        {
            if (_currentTransaction == null)
            {
                throw new InvalidOperationException("No active transaction to commit.");
            }

            try
            {
                await _currentTransaction.CommitAsync(ct);
                _logger.LogInformation("Explicit transaction committed");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to commit transaction");
                await SafeRollbackAsync(_currentTransaction, ct);
                throw;
            }
            finally
            {
                await _currentTransaction.DisposeAsync();
                _currentTransaction = null;
            }
        }

        public async ValueTask DisposeAsync()
        {
            if (_currentTransaction != null)
            {
                await _currentTransaction.DisposeAsync();
                _currentTransaction = null;
            }
        }

        public async Task RollbackTransactionAsync(CancellationToken ct = default)
        {
            if (_currentTransaction == null) return;

            try
            {
                await _currentTransaction.RollbackAsync(ct);
                _logger.LogWarning("Explicit transaction rolled back");
            }
            finally
            {
                await _currentTransaction.DisposeAsync();
                _currentTransaction = null;
                _dbContext.ChangeTracker.Clear();
            }
        }

        private async Task<int> SaveChangesAsync(CancellationToken ct)
        {
            try
            {
                return await _dbContext.SaveChangesAsync(ct);
            }
            catch (DbUpdateConcurrencyException ex)
            {
                _logger.LogError(ex, "Concurrency conflict detected");
                throw new RepositoryConcurrencyException(
                    "A concurrency conflict occurred.", ex);
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(ex, "Database update error");
                throw new RepositoryException(
                    "A database update error occurred.", ex);
            }
        }

        private async Task SafeRollbackAsync(IDbContextTransaction transaction, CancellationToken ct)
        {
            try
            {
                await transaction.RollbackAsync(ct);
                _logger.LogWarning("Transaction rolled back");
            }
            catch (Exception rollbackEx)
            {
                _logger.LogCritical(rollbackEx,
                    "Rollback failed. Manual intervention may be required.");
            }
        }

        private Exception MapException(Exception ex)
        {
            return ex switch
            {
                DbUpdateConcurrencyException concurrencyEx =>
                    new RepositoryConcurrencyException(
                        "A concurrency conflict occurred.", concurrencyEx),

                DbUpdateException dbEx =>
                    new RepositoryException(
                        "A database update error occurred.", dbEx),

                OperationCanceledException cancelEx =>
                    new RepositoryException(
                        "Transaction was cancelled.", cancelEx),

                _ => new RepositoryException(
                    "An unexpected error occurred during transaction.", ex)
            };
        }
    }
}
