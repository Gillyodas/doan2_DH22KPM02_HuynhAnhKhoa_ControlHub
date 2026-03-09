namespace ControlHub.Application.TokenManagement.Interfaces.Generate
{
    public interface IEmailConfirmationTokenGenerator
    {
        string Generate(string userId, string email);
    }
}
