using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.Extensions.Configuration;

namespace ControlHub.Infrastructure.Tokens.Generate
{
    public class PasswordResetTokenGenerator : TokenGeneratorBase
    {
        public PasswordResetTokenGenerator(IConfiguration config) : base(config) { }

        public string Generate(string userId)
        {
            var claims = new List<Claim>
        {
            new Claim(JwtRegisteredClaimNames.Sub, userId),
            new Claim("purpose", "password_reset")
        };

            return GenerateToken(claims, TimeSpan.FromMinutes(30)); // TTL 30'
        }
    }
}
