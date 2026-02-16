namespace ControlHub.Application.Tokens.Interfaces.Generate
{
    public interface IEmailConfirmationTokenGenerator
    {
        string Generate(string userId, string email);
    }
}
