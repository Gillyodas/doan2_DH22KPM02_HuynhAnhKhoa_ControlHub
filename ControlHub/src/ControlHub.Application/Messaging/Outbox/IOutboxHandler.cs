namespace ControlHub.Application.Messaging.Outbox
{
    public interface IOutboxHandler
    {
        OutboxMessageType Type { get; }
        Task HandleAsync(string payload, CancellationToken ct);
    }
}
