using System.Security.Claims;

namespace ControlHub.Application.TokenManagement.Interfaces
{
    public interface ITokenVerifier
    {
        ClaimsPrincipal? Verify(string token);
    }
}
