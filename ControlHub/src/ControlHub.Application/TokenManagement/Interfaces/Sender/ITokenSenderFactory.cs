using ControlHub.Domain.Identity.Enums;

namespace ControlHub.Application.TokenManagement.Interfaces.Sender
{
    public interface ITokenSenderFactory
    {
        ITokenSender? Get(IdentifierType type);
    }
}
