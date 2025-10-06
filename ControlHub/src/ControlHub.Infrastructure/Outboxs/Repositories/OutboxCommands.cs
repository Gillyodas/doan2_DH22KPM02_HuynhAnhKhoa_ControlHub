using ControlHub.Application.OutBoxs.Repositories;
using ControlHub.Domain.Outboxs;
using ControlHub.Infrastructure.Persistence;

namespace ControlHub.Infrastructure.Outboxs.Repositories
{
    public class OutboxCommands : IOutboxCommands
    {
        private readonly AppDbContext _db;
        public OutboxCommands(AppDbContext db)
        {
            _db = db;
        }
        public async Task AddAsync(OutboxMessage domainOutbox, CancellationToken cancellationToken)
        {
            var entity = OutboxMapper.ToEntity(domainOutbox);
            await _db.OutboxMessages.AddAsync(entity, cancellationToken);
        }
    }
}
