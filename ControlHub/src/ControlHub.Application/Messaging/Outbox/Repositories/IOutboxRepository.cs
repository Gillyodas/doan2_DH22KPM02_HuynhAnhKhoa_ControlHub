namespace ControlHub.Application.Messaging.Outbox.Repositories
{
    public interface IOutboxRepository
    {
        Task AddAsync(OutboxMessage domainOutbox, CancellationToken cancellationToken);
        Task SaveChangesAsync(CancellationToken cancellationToken);
    }
}
