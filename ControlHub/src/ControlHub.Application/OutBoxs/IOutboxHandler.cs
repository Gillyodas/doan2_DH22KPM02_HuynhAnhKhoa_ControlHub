using ControlHub.Domain.Outboxs;

namespace ControlHub.Application.OutBoxs
{
    public interface IOutboxHandler
    {
        OutboxMessageType Type { get; }
        Task HandleAsync(string payload, CancellationToken ct);
    }
}
