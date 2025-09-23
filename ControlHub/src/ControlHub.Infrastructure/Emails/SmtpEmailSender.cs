using System.Net;
using System.Net.Mail;
using ControlHub.Application.Emails.Interfaces;
using Microsoft.Extensions.Configuration;

namespace ControlHub.Infrastructure.Emails
{
    public class SmtpEmailSender : IEmailSender
    {
        private readonly IConfiguration _config;

        public SmtpEmailSender(IConfiguration config)
        {
            _config = config;
        }

        public async Task SendEmailAsync(string to, string subject, string body, bool isHtml = true)
        {
            var smtpSection = _config.GetSection("Smtp");
            var host = smtpSection["Host"];
            var port = int.Parse(smtpSection["Port"] ?? "25");
            var username = smtpSection["Username"];
            var password = smtpSection["Password"];
            var from = smtpSection["From"];

            using var client = new SmtpClient(host, port)
            {
                Credentials = new NetworkCredential(username, password),
                EnableSsl = true
            };

            var message = new MailMessage(from!, to, subject, body)
            {
                IsBodyHtml = isHtml
            };

            await client.SendMailAsync(message);
        }
    }
}
