using ControlHub.Application.Messaging.Outbox;

namespace ControlHub.Infrastructure.Messaging.Outbox.Handler
{
    public class OutboxHandlerFactory
    {
        private readonly Dictionary<OutboxMessageType, IOutboxHandler> _handlers;

        public OutboxHandlerFactory(IEnumerable<IOutboxHandler> handlers)
        {
            _handlers = handlers.ToDictionary(h => h.Type, h => h);
        }
        //TODO: Thay d?i t? kh�ng virtual th�nh c� virtual d? c� th? override trong unit test
        public virtual IOutboxHandler? Get(OutboxMessageType type)
            => _handlers.TryGetValue(type, out var handler) ? handler : null;
    }

}
