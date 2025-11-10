using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using ControlHub.Application.Tokens.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;

namespace ControlHub.Infrastructure.Tokens.Generate
{
    public class TokenVerifier : ITokenVerifier
    {
        private readonly IConfiguration _config;
        private readonly ILogger<TokenVerifier> _logger;

        static TokenVerifier()
        {
            // Clear mapping once globally
            JwtSecurityTokenHandler.DefaultInboundClaimTypeMap.Clear();
            JwtSecurityTokenHandler.DefaultOutboundClaimTypeMap.Clear();
        }

        public TokenVerifier(IConfiguration config, ILogger<TokenVerifier> logger)
        {
            _config = config;
            _logger = logger;
        }

        // ====== STATIC METHOD DÙNG CHO AddJwtBearer() ======
        public static TokenValidationParameters GetValidationParameters(IConfiguration? config = null)
        {
            // Nếu config bị null (ví dụ gọi từ static Program.cs) thì đọc trực tiếp từ environment
            string issuer = config?["Jwt:Issuer"] ?? Environment.GetEnvironmentVariable("Jwt__Issuer") ?? "UnknownIssuer";
            string audience = config?["Jwt:Audience"] ?? Environment.GetEnvironmentVariable("Jwt__Audience") ?? "UnknownAudience";
            string key = config?["Jwt:Key"] ?? Environment.GetEnvironmentVariable("Jwt__Key") ?? throw new InvalidOperationException("JWT key is missing.");

            return new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key)),

                ValidateIssuer = true,
                ValidIssuer = issuer,

                ValidateAudience = true,
                ValidAudience = audience,

                ValidateLifetime = true,
                ClockSkew = TimeSpan.Zero, // Không cho phép lệch thời gian

                // Chặn giả mạo thuật toán khác
                ValidAlgorithms = new[] { SecurityAlgorithms.HmacSha256 }
            };
        }

        // ====== VERIFY TOKEN TRỰC TIẾP ======
        public ClaimsPrincipal? Verify(string token)
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            var key = Encoding.UTF8.GetBytes(_config["Jwt:Key"]!);

            var validationParams = new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(key),

                ValidateIssuer = true,
                ValidIssuer = _config["Jwt:Issuer"],

                ValidateAudience = true,
                ValidAudience = _config["Jwt:Audience"],

                ValidateLifetime = true,
                ClockSkew = TimeSpan.Zero,

                // Chặn thuật toán giả mạo
                ValidAlgorithms = new[] { SecurityAlgorithms.HmacSha256 }
            };

            try
            {
                var principal = tokenHandler.ValidateToken(token, validationParams, out var validatedToken);

                if (validatedToken is not JwtSecurityToken jwt)
                {
                    _logger.LogWarning("Invalid JWT structure");
                    return null;
                }

                if (!jwt.Header.Alg.Equals(SecurityAlgorithms.HmacSha256, StringComparison.InvariantCultureIgnoreCase))
                {
                    _logger.LogWarning("Invalid JWT signing algorithm: {Alg}", jwt.Header.Alg);
                    return null;
                }

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