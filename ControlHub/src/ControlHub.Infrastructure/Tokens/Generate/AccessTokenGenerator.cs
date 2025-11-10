using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using ControlHub.Application.Tokens.Interfaces.Generate;
using Microsoft.Extensions.Configuration;

namespace ControlHub.Infrastructure.Tokens.Generate
{
    public class AccessTokenGenerator : TokenGeneratorBase, IAccessTokenGenerator
    {
        public AccessTokenGenerator(IConfiguration config) : base(config) { }

        public string Generate(string accId, string identifier, IEnumerable<string> roles)
        {
            var claims = new List<Claim>
        {
            new Claim(JwtRegisteredClaimNames.Sub, accId),
            new Claim(ClaimTypes.NameIdentifier, identifier),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };
            claims.AddRange(roles.Select(r => new Claim(ClaimTypes.Role, r)));

            return GenerateToken(claims, TimeSpan.FromMinutes(15)); // TTL 15'
        }
    }
}
