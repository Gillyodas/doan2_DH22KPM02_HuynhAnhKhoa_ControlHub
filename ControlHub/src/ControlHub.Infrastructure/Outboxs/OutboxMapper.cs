using ControlHub.Domain.Outboxs;

namespace ControlHub.Infrastructure.Outboxs
{

    public static class OutboxMapper
    {
        public static OutboxMessageEntity ToEntity(OutboxMessage domain)
        {
            return new OutboxMessageEntity
            {
                Id = domain.Id,
                OccurredOn = domain.OccurredOn,
                Type = domain.Type,
                Payload = domain.Payload,
                Processed = domain.Processed,
                ProcessedOn = domain.ProcessedOn,
                Error = domain.Error
            };
        }

        public static OutboxMessage ToDomain(OutboxMessageEntity entity)
        {
            return OutboxMessage.Rehydrate(
                entity.Id,
                entity.OccurredOn,
                entity.Type,
                entity.Payload,
                entity.Processed,
                entity.ProcessedOn,
                entity.Error
            );
        }
    }
}