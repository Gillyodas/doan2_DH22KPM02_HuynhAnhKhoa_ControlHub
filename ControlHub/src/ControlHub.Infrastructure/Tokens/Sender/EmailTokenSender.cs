using ControlHub.Application.Emails.Interfaces;
using ControlHub.Application.Tokens.Interfaces.Sender;
using ControlHub.Domain.Identity.Enums;

namespace ControlHub.Infrastructure.Tokens.Sender
{
    internal class EmailTokenSender : ITokenSender
    {
        private readonly IEmailSender _emailSender;
        public IdentifierType Type => IdentifierType.Email;

        public EmailTokenSender(IEmailSender emailSender)
        {
            _emailSender = emailSender;
        }

        public async Task SendAsync(string identifier, string token, CancellationToken ct)
        {
            var resetLink = $"https://localhost:7110/swagger/index.html?token={token}";
            var subject = "Reset your password";
            var body = $"Click this link to reset your password: <a href='{resetLink}'>Reset Password</a>";

            await _emailSender.SendEmailAsync(identifier, subject, body);
        }
    }
}
