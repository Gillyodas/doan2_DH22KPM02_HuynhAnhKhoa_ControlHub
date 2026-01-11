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
        //TODO: Thay đổi từ không virtual thành có virtual để có thể override trong unit test
        public virtual IOutboxHandler? Get(OutboxMessageType type)
            => _handlers.TryGetValue(type, out var handler) ? handler : null;
    }

}
