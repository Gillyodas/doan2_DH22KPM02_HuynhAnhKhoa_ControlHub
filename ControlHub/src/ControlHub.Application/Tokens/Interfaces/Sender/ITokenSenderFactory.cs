using ControlHub.Domain.Identity.Enums;

namespace ControlHub.Application.Tokens.Interfaces.Sender
{
    public interface ITokenSenderFactory
    {
        ITokenSender? Get(IdentifierType type);
    }
}
