using System.Security.Claims;

namespace ControlHub.Application.Tokens.Interfaces
{
    public interface ITokenVerifier
    {
        ClaimsPrincipal? Verify(string token);
    }
}
