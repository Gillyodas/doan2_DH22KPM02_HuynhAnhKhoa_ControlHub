using ControlHub.Application.Accounts.Identifiers.Interfaces;
using ControlHub.Domain.Accounts.Enums;
using ControlHub.Domain.Accounts.Identifiers.Interfaces;

namespace ControlHub.Infrastructure.Identifiers.Validator
{
    public class IdentifierValidatorFactory : IIdentifierValidatorFactory
    {
        private readonly Dictionary<IdentifierType, IIdentifierValidator> _map;
        public IdentifierValidatorFactory(IEnumerable<IIdentifierValidator> validators)
        {
            _map = validators.ToDictionary(v => v.Type, v => v);
        }
        public IIdentifierValidator? Get(IdentifierType type) => _map.TryGetValue(type, out var v) ? v : null;
    }
}
