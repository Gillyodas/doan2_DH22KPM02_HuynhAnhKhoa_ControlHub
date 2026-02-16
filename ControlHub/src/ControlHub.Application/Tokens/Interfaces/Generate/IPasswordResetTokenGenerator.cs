namespace ControlHub.Application.Tokens.Interfaces.Generate
{
    public interface IPasswordResetTokenGenerator
    {
        string Generate(string userId);
    }
}
