using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using ControlHub.Application.Tokens.Interfaces.Generate;
using Microsoft.Extensions.Configuration;

namespace ControlHub.Infrastructure.Tokens.Generate
{
    public class AccessTokenGenerator : TokenGeneratorBase, IAccessTokenGenerator
    {
        public AccessTokenGenerator(IConfiguration config) : base(config) { }

        public string Generate(string accountId, string identifier, string roleId)
        {
            var claims = new List<Claim>
            {
                // 1. LUU ID VÀO C? "sub" VÀ "NameIdentifier"
                // "sub" là chu?n JWT qu?c t?
                new Claim(JwtRegisteredClaimNames.Sub, accountId),
        
                // "NameIdentifier" là chu?n c?a .NET Identity d? d?nh danh User ID
                new Claim(ClaimTypes.NameIdentifier, accountId),

                new Claim(ClaimTypes.Name, identifier),

                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
                new Claim(ClaimTypes.Role, roleId)
            };

            return GenerateToken(claims, TimeSpan.FromMinutes(15));
        }
    }
}
