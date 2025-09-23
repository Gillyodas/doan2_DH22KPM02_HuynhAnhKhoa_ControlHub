using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using ControlHub.Application.Tokens.Interfaces;
using Microsoft.Extensions.Configuration;

namespace ControlHub.Infrastructure.Tokens.Generate
{
    public class EmailConfirmationTokenGenerator : TokenGeneratorBase, IEmailConfirmationTokenGenerator
    {
        public EmailConfirmationTokenGenerator(IConfiguration config) : base(config) { }

        public string Generate(string userId, string email)
        {
            var claims = new List<Claim>
        {
            new Claim(JwtRegisteredClaimNames.Sub, userId),
            new Claim(JwtRegisteredClaimNames.Email, email),
            new Claim("purpose", "email_confirmation")
        };

            return GenerateToken(claims, TimeSpan.FromHours(24)); // TTL 24h
        }
    }

}
