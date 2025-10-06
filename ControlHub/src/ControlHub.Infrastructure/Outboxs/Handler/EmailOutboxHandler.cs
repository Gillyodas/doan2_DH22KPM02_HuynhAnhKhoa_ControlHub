using System.Text.Json;
using ControlHub.Application.Emails.Interfaces;
using ControlHub.Application.OutBoxs;
using ControlHub.Domain.Outboxs;

namespace ControlHub.Infrastructure.Outboxs.Handler
{
    public class EmailOutboxHandler : IOutboxHandler
    {
        private readonly IEmailSender _emailSender;

        public EmailOutboxHandler(IEmailSender emailSender)
        {
            _emailSender = emailSender;
        }

        public OutboxMessageType Type => OutboxMessageType.Email;

        public async Task HandleAsync(string payload, CancellationToken ct)
        {
            var emailPayload = JsonSerializer.Deserialize<EmailPayload>(payload)!;
            await _emailSender.SendEmailAsync(emailPayload.To, emailPayload.Subject, emailPayload.Body);
        }

        private record EmailPayload(string To, string Subject, string Body);
    }
}
