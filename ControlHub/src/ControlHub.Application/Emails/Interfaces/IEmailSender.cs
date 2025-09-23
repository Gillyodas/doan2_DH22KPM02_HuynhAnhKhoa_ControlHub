namespace ControlHub.Application.Emails.Interfaces
{
    public interface IEmailSender
    {
        Task SendEmailAsync(string to, string subject, string body, bool isHtml = true);
    }
}
