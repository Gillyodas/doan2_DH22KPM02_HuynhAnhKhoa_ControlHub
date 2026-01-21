using ControlHub.Application.OutBoxs.Repositories;
using ControlHub.Domain.Outboxs;
using ControlHub.Infrastructure.Persistence;
using ControlHub.SharedKernel.Common.Exceptions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace ControlHub.Infrastructure.Outboxs.Repositories
{
    internal class OutboxRepository : IOutboxRepository
    {
        private readonly AppDbContext _db;
        private readonly ILogger<OutboxRepository> _logger;

        public OutboxRepository(AppDbContext db, ILogger<OutboxRepository> logger)
        {
            _db = db;
            _logger = logger;
        }

        public async Task AddAsync(OutboxMessage domainOutbox, CancellationToken cancellationToken)
        {
            try
            {
                await _db.OutboxMessages.AddAsync(domainOutbox, cancellationToken);
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(ex, "Failed to add OutboxMessage {Id}", domainOutbox.Id);
                throw new RepositoryException("Error adding outbox message to database.", ex);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error while adding OutboxMessage {Id}", domainOutbox.Id);
                throw new RepositoryException("Unexpected error while adding outbox message.", ex);
            }
        }

        public async Task SaveChangesAsync(CancellationToken cancellationToken)
        {
            try
            {
                await _db.SaveChangesAsync(cancellationToken);
            }
            catch (DbUpdateConcurrencyException ex)
            {
                _logger.LogError(ex, "Concurrency conflict during Outbox SaveChanges");
                throw new RepositoryConcurrencyException("Concurrency conflict while saving outbox messages.", ex);
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(ex, "Database update error during Outbox SaveChanges");
                throw new RepositoryException("Database update error while saving outbox messages.", ex);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error during Outbox SaveChanges");
                throw new RepositoryException("Unexpected error during outbox save operation.", ex);
            }
        }
    }
}