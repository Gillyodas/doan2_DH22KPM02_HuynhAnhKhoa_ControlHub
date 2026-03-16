using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using ControlHub.Application.TokenManagement.Interfaces.Generate;
using Microsoft.Extensions.Configuration;

namespace ControlHub.Infrastructure.TokenManagement.Services.Generate
{
    public class AccessTokenGenerator : TokenGeneratorBase, IAccessTokenGenerator
    {
        private readonly IConfiguration _config;

        public AccessTokenGenerator(IConfiguration config) : base(config)
        {
            _config = config;
        }

        public string Generate(string accountId, string identifier, string roleId)
        {
            var claims = new List<Claim>
        {
            new Claim(JwtRegisteredClaimNames.Sub, accountId),
            new Claim(ClaimTypes.NameIdentifier, accountId),
            new Claim(ClaimTypes.Name, identifier),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new Claim(ClaimTypes.Role, roleId)
        };

            var minutes = int.Parse(_config["TokenSettings:AccessTokenMinutes"] ?? "15");
            return GenerateToken(claims, TimeSpan.FromMinutes(minutes));
        }
    }
}
