using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging; // Use ILogger
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace ControlHub.Infrastructure.Tokens
{
    // ?? CRITICAL: Must be IConfigureNamedOptions to support named schemes like "Bearer"
    internal class ConfigureJwtBearerOptions : IConfigureNamedOptions<JwtBearerOptions>
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<ConfigureJwtBearerOptions> _logger;

        public ConfigureJwtBearerOptions(IConfiguration configuration, ILogger<ConfigureJwtBearerOptions> logger)
        {
            _configuration = configuration;
            _logger = logger;
        }

        // 1. This method is called by the framework for Named Options
        public void Configure(string? name, JwtBearerOptions options)
        {
            // Only configure if the scheme matches "Bearer" (JwtBearerDefaults.AuthenticationScheme)
            // or if name is null (global default)
            if (string.IsNullOrEmpty(name) || name == JwtBearerDefaults.AuthenticationScheme)
            {
                Configure(options);
            }
        }

        // 2. Main configuration logic
        public void Configure(JwtBearerOptions options)
        {
            var issuer = _configuration["Jwt:Issuer"];
            var audience = _configuration["Jwt:Audience"];
            var key = _configuration["Jwt:Key"];

            // Fail Fast: Check for missing config immediately to avoid cryptic "Signature validation failed" errors later
            if (string.IsNullOrEmpty(key) || string.IsNullOrEmpty(issuer) || string.IsNullOrEmpty(audience))
            {
                _logger.LogError("? [JWT Config] Missing configuration in appsettings.json. Check Jwt:Issuer, Jwt:Audience, Jwt:Key.");
                throw new InvalidOperationException("JWT Configuration is missing.");
            }

            _logger.LogInformation("?? [JWT Config] Successfully loaded. Issuer: {Issuer}", issuer);

            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key)),

                ValidateIssuer = true,
                ValidIssuer = issuer,

                ValidateAudience = true,
                ValidAudience = audience,

                ValidateLifetime = true,
                ClockSkew = TimeSpan.Zero,

                ValidAlgorithms = new[] { SecurityAlgorithms.HmacSha256 }
            };
        }
    }
}
