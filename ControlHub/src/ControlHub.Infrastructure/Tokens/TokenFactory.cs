using ControlHub.Application.Tokens.Interfaces;
using ControlHub.Domain.Tokens;
using ControlHub.Domain.Tokens.Enums;
using Microsoft.Extensions.Options;

namespace ControlHub.Infrastructure.Tokens
{
    public class TokenFactory : ITokenFactory
    {
        private readonly TokenSettings _settings;

        public TokenFactory(IOptions<TokenSettings> settings)
        {
            _settings = settings.Value;
        }

        public Token Create(Guid accountId, string value, TokenType type)
        {
            var now = DateTime.UtcNow;
            var expiry = type switch
            {
                TokenType.ResetPassword => now.AddMinutes(_settings.ResetPasswordMinutes),
                TokenType.VerifyEmail => now.AddHours(_settings.VerifyEmailHours),
                TokenType.RefreshToken => now.AddDays(_settings.RefreshTokenDays),
                TokenType.AccessToken => now.AddMinutes(_settings.AccessTokenMinutes),
                _ => now.AddHours(1)
            };

            return Token.Create(accountId, value, type, expiry);
        }
    }
}