namespace ControlHub.Application.Tokens.Interfaces.Generate
{
    public interface IAccessTokenGenerator
    {
        string Generate(string accId, string identifier, string roleId);
    }
}
