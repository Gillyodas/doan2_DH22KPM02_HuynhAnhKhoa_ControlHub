using System.Net;
using System.Net.Mail;
using ControlHub.Application.Emails.Interfaces;
using Microsoft.Extensions.Configuration;

namespace ControlHub.Infrastructure.Emails
{
    internal class SmtpEmailSender : IEmailSender
    {
        private readonly IConfiguration _config;

        public SmtpEmailSender(IConfiguration config)
        {
            _config = config ?? throw new ArgumentNullException(nameof(config));
        }

        public async Task SendEmailAsync(string to, string subject, string body, bool isHtml = true)
        {
            // Validate input parameters
            if (string.IsNullOrWhiteSpace(to))
                throw new ArgumentException("To email address is required", nameof(to));

            if (string.IsNullOrWhiteSpace(subject))
                throw new ArgumentException("Subject is required", nameof(subject));

            if (string.IsNullOrWhiteSpace(body))
                throw new ArgumentException("Body is required", nameof(body));

            // Validate email format
            try
            {
                var mailAddress = new MailAddress(to);
            }
            catch (FormatException)
            {
                throw new FormatException("Invalid email address format");
            }

            // Get and validate SMTP configuration
            var smtpSection = _config.GetSection("Smtp");
            var host = smtpSection["Host"];
            var portStr = smtpSection["Port"];
            var username = smtpSection["Username"];
            var password = smtpSection["Password"];
            var from = smtpSection["From"];

            // Validate required configuration values
            if (string.IsNullOrWhiteSpace(host))
                throw new ArgumentNullException("Smtp:Host", "SMTP host is required");

            if (string.IsNullOrWhiteSpace(username))
                throw new ArgumentNullException("Smtp:Username", "SMTP username is required");

            if (string.IsNullOrWhiteSpace(password))
                throw new ArgumentNullException("Smtp:Password", "SMTP password is required");

            if (string.IsNullOrWhiteSpace(from))
                throw new ArgumentNullException("Smtp:From", "SMTP from address is required");

            // Parse and validate port
            int port;
            if (string.IsNullOrWhiteSpace(portStr))
            {
                port = 25; // Default port
            }
            else
            {
                try
                {
                    port = int.Parse(portStr);
                }
                catch (FormatException)
                {
                    throw new FormatException("Invalid port format");
                }
            }

            // Validate port range
            if (port < 1 || port > 65535)
                throw new ArgumentOutOfRangeException("Smtp:Port", "Port must be between 1 and 65535");

            // Validate from address format
            try
            {
                var fromAddress = new MailAddress(from);
            }
            catch (FormatException)
            {
                throw new FormatException("Invalid from email address format");
            }

            // Validate subject and body length (reasonable limits)
            if (subject.Length > 1000)
                throw new ArgumentException("Subject is too long (max 1000 characters)");

            if (body.Length > 1000000) // 1MB limit
                throw new ArgumentException("Body is too long (max 1MB)");

            using var client = new SmtpClient(host, port)
            {
                Credentials = new NetworkCredential(username, password),
                EnableSsl = true
            };

            var message = new MailMessage(from, to, subject, body)
            {
                IsBodyHtml = isHtml
            };

            try
            {
                await client.SendMailAsync(message);
            }
            catch (SmtpException)
            {
                // Re-throw SMTP exceptions to preserve the original behavior
                throw;
            }
            catch (Exception ex)
            {
                // Wrap other exceptions in SmtpException for consistency
                throw new SmtpException("Failed to send email", ex);
            }
        }
    }
}
