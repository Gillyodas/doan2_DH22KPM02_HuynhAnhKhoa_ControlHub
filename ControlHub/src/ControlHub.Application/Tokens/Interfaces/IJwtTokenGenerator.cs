using System.Security.Claims;

namespace ControlHub.Application.Tokens.Interfaces
{
    public interface ITokenGenerator
    {
        string GenerateToken(IEnumerable<Claim> claims, TimeSpan expiry);
    }
}
