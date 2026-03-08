using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using ControlHub.Application.Tokens.Interfaces;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace ControlHub.Infrastructure.TokenManagement.Services.Generate
{
    internal class TokenVerifier : ITokenVerifier
    {
        private readonly ILogger<TokenVerifier> _logger;
        private readonly TokenValidationParameters _validationParameters;
        private static readonly JwtSecurityTokenHandler _tokenHandler = new();

        static TokenVerifier()
        {
            JwtSecurityTokenHandler.DefaultInboundClaimTypeMap.Clear();
            JwtSecurityTokenHandler.DefaultOutboundClaimTypeMap.Clear();
        }

        // THAY �?I L?N:
        // Ch�ng ta inject IOptionsMonitor d? l?y TokenValidationParameters
        // m� ConfigureJwtBearerOptions d� t?o.
        public TokenVerifier(ILogger<TokenVerifier> logger, IOptionsMonitor<JwtBearerOptions> jwtOptions)
        {
            _logger = logger;

            // L?y c?u h�nh c?a scheme "Bearer" (m?c d?nh)
            var options = jwtOptions.Get(JwtBearerDefaults.AuthenticationScheme);

            // Sao ch�p (clone) d? d?m b?o an to�n thread v� kh�ng b? thay d?i
            _validationParameters = options.TokenValidationParameters.Clone();
        }

        public ClaimsPrincipal? Verify(string token)
        {
            // Kh�ng c?n "new TokenValidationParameters" n?a.
            // Ch�ng ta d�ng _validationParameters d� du?c inject.
            // Logic c?a b?n d� du?c DRY (Don't Repeat Yourself)!

            try
            {
                var principal = _tokenHandler.ValidateToken(token, _validationParameters, out var validatedToken);

                if (validatedToken is not JwtSecurityToken jwt)
                {
                    _logger.LogWarning("Invalid JWT structure");
                    return null;
                }

                // Vi?c ki?m tra 'alg' n�y d� du?c
                // _validationParameters.ValidAlgorithms lo r?i,
                // nhung c?n th?n 2 l?n cung t?t.

                return principal;
            }
            catch (SecurityTokenExpiredException ex)
            {
                _logger.LogWarning(ex, "Token expired");
                return null;
            }
            catch (SecurityTokenInvalidSignatureException ex)
            {
                _logger.LogWarning(ex, "Invalid token signature");
                return null;
            }
            catch (SecurityTokenException ex)
            {
                _logger.LogWarning(ex, "Invalid token");
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error verifying token");
                return null;
            }
        }
    }
}
