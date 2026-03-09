namespace ControlHub.Application.TokenManagement.Interfaces.Generate
{
    public interface IPasswordResetTokenGenerator
    {
        string Generate(string userId);
    }
}
