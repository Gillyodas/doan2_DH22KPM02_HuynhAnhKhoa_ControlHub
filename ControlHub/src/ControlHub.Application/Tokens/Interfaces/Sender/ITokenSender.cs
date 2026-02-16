using ControlHub.Domain.Identity.Enums;

namespace ControlHub.Application.Tokens.Interfaces.Sender
{
    public interface ITokenSender
    {
        IdentifierType Type { get; }
        Task SendAsync(string identifier, string token, CancellationToken ct);
    }
}
