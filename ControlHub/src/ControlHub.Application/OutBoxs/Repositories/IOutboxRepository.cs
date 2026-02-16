using ControlHub.Domain.Outboxs;

namespace ControlHub.Application.OutBoxs.Repositories
{
    public interface IOutboxRepository
    {
        Task AddAsync(OutboxMessage domainOutbox, CancellationToken cancellationToken);
        Task SaveChangesAsync(CancellationToken cancellationToken);
    }
}
