using ControlHub.Application.Tokens.Interfaces;
using ControlHub.Application.Tokens.Interfaces.Generate;
using ControlHub.Application.Tokens.Interfaces.Repositories;
using ControlHub.Application.Tokens.Interfaces.Sender;
using ControlHub.Infrastructure.Tokens;
using ControlHub.Infrastructure.Tokens.Generate;
using ControlHub.Infrastructure.Tokens.Repositories;
using ControlHub.Infrastructure.Tokens.Sender;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace ControlHub.Infrastructure.DependencyInjection;

internal static class TokenExtensions
{
    internal static IServiceCollection AddControlHubTokens(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.Configure<TokenSettings>(configuration.GetSection("TokenSettings"));

        services.AddScoped<IAccessTokenGenerator, AccessTokenGenerator>();
        services.AddScoped<IRefreshTokenGenerator, RefreshTokenGenerator>();
        services.AddScoped<IPasswordResetTokenGenerator, PasswordResetTokenGenerator>();
        services.AddScoped<IEmailConfirmationTokenGenerator, EmailConfirmationTokenGenerator>();

        services.AddScoped<ITokenSender, EmailTokenSender>();
        services.AddScoped<ITokenSender, SmsTokenSender>();
        services.AddScoped<ITokenSenderFactory, TokenSenderFactory>();
        services.AddScoped<ITokenFactory, TokenFactory>();
        services.AddScoped<ITokenQueries, TokenQueries>();
        services.AddScoped<ITokenRepository, TokenRepository>();
        services.AddScoped<ITokenVerifier, TokenVerifier>();

        return services;
    }
}