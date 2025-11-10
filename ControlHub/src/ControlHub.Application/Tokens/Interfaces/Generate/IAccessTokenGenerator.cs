namespace ControlHub.Application.Tokens.Interfaces.Generate
{
    public interface IAccessTokenGenerator
    {
        string Generate(string accId, string identifier, IEnumerable<string> roles);
    }
}
