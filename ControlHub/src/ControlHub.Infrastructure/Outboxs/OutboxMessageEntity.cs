using ControlHub.Domain.Outboxs;

namespace ControlHub.Infrastructure.Outboxs
{
    public class OutboxMessageEntity
    {
        public Guid Id { get; set; }
        public DateTime OccurredOn { get; set; } = DateTime.UtcNow;
        public OutboxMessageType Type { get; set; }      // loại message, ví dụ: "Email"
        public string Payload { get; set; } = null!;   // JSON nội dung message
        public bool Processed { get; set; } = false;
        public DateTime? ProcessedOn { get; set; }
        public string? Error { get; set; }
    }
}
