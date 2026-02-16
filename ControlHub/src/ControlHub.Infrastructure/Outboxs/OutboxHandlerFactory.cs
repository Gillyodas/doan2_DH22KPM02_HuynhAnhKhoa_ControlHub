using ControlHub.Application.OutBoxs;
using ControlHub.Domain.Outboxs;

namespace ControlHub.Infrastructure.Outboxs
{
    public class OutboxHandlerFactory
    {
        private readonly Dictionary<OutboxMessageType, IOutboxHandler> _handlers;

        public OutboxHandlerFactory(IEnumerable<IOutboxHandler> handlers)
        {
            _handlers = handlers.ToDictionary(h => h.Type, h => h);
        }
        //TODO: Thay d?i t? không virtual thành có virtual d? có th? override trong unit test
        public virtual IOutboxHandler? Get(OutboxMessageType type)
            => _handlers.TryGetValue(type, out var handler) ? handler : null;
    }

}
