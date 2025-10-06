using ControlHub.Domain.Outboxs;

namespace ControlHub.Application.OutBoxs.Repositories
{
    public interface IOutboxCommands
    {
        public Task AddAsync(OutboxMessage domainOutbox, CancellationToken cancellationToken);
    }
}
