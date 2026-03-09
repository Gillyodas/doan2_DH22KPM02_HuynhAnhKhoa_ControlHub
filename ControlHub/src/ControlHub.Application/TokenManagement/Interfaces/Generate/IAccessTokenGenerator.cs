namespace ControlHub.Application.TokenManagement.Interfaces.Generate
{
    public interface IAccessTokenGenerator
    {
        string Generate(string accId, string identifier, string roleId);
    }
}
