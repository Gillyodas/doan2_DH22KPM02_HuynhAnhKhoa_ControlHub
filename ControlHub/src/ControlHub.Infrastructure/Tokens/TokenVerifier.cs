using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using ControlHub.Application.Tokens.Interfaces;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace ControlHub.Infrastructure.Tokens.Generate
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

        // THAY ĐỔI LỚN:
        // Chúng ta inject IOptionsMonitor để lấy TokenValidationParameters
        // mà ConfigureJwtBearerOptions đã tạo.
        public TokenVerifier(ILogger<TokenVerifier> logger, IOptionsMonitor<JwtBearerOptions> jwtOptions)
        {
            _logger = logger;

            // Lấy cấu hình của scheme "Bearer" (mặc định)
            var options = jwtOptions.Get(JwtBearerDefaults.AuthenticationScheme);

            // Sao chép (clone) để đảm bảo an toàn thread và không bị thay đổi
            _validationParameters = options.TokenValidationParameters.Clone();
        }

        public ClaimsPrincipal? Verify(string token)
        {
            // Không cần "new TokenValidationParameters" nữa.
            // Chúng ta dùng _validationParameters đã được inject.
            // Logic của bạn đã được DRY (Don't Repeat Yourself)!

            try
            {
                var principal = _tokenHandler.ValidateToken(token, _validationParameters, out var validatedToken);

                if (validatedToken is not JwtSecurityToken jwt)
                {
                    _logger.LogWarning("Invalid JWT structure");
                    return null;
                }

                // Việc kiểm tra 'alg' này đã được
                // _validationParameters.ValidAlgorithms lo rồi,
                // nhưng cẩn thận 2 lần cũng tốt.

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