namespace ControlHub.Domain.Outboxs
{
    public class OutboxMessage
    {
        public Guid Id { get; private set; }
        public DateTime OccurredOn { get; private set; }
        public OutboxMessageType Type { get; private set; }
        public string Payload { get; private set; }
        public bool Processed { get; private set; }
        public DateTime? ProcessedOn { get; private set; }
        public string? Error { get; private set; }

        private OutboxMessage() { } // EF / serialization

        private OutboxMessage(Guid id, OutboxMessageType type, string payload)
        {
            Id = id;
            Type = type;
            Payload = payload ?? throw new ArgumentNullException(nameof(payload));
            OccurredOn = DateTime.UtcNow;
            Processed = false;
        }

        public static OutboxMessage Create(OutboxMessageType type, string payload)
            => new OutboxMessage(Guid.NewGuid(), type, payload);

        public static OutboxMessage Rehydrate(
            Guid id,
            DateTime occurredOn,
            OutboxMessageType type,
            string payload,
            bool processed,
            DateTime? processedOn,
            string? error)
        {
            return new OutboxMessage
            {
                Id = id,
                OccurredOn = occurredOn,
                Type = type,
                Payload = payload,
                Processed = processed,
                ProcessedOn = processedOn,
                Error = error
            };
        }

        public void MarkProcessed()
        {
            Processed = true;
            ProcessedOn = DateTime.UtcNow;
            Error = null;
        }

        public void MarkFailed(string error)
        {
            Processed = false;
            Error = error;
        }
    }
}