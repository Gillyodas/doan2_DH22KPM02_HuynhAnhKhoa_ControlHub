using ControlHub.Application.Accounts.Identifiers.Interfaces;
using ControlHub.Domain.Accounts.Enums;

namespace ControlHub.Domain.Accounts.Identifiers.Interfaces
{
    public interface IIdentifierValidatorFactory
    {
        IIdentifierValidator? Get(IdentifierType type);
    }
}
