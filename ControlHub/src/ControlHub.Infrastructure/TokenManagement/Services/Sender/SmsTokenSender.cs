using ControlHub.Application.TokenManagement.Interfaces.Sender;
using ControlHub.Domain.Identity.Enums;

namespace ControlHub.Infrastructure.TokenManagement.Services.Sender
{
    internal class SmsTokenSender : ITokenSender
    {
        public IdentifierType Type => IdentifierType.Phone;

        public Task SendAsync(string identifier, string token, CancellationToken ct)
        {
            // TODO: implement real SMS sending
            return Task.CompletedTask;
        }
    }
}
