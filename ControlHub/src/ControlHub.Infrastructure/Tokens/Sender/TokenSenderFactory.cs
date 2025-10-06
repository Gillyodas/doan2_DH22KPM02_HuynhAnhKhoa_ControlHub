using ControlHub.Application.Tokens.Interfaces.Sender;
using ControlHub.Domain.Accounts.Enums;

namespace ControlHub.Infrastructure.Tokens.Sender
{
    public class TokenSenderFactory : ITokenSenderFactory
    {
        private readonly Dictionary<IdentifierType, ITokenSender> _map;

        public TokenSenderFactory(IEnumerable<ITokenSender> sender)
        {
            _map = sender.ToDictionary(s => s.Type, s => s);
        }
        public ITokenSender? Get(IdentifierType type) => _map.TryGetValue(type, out var s) ? s : null;
    }
}
