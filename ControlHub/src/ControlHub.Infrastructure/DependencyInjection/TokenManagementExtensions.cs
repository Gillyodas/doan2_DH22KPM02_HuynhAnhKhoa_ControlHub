using ControlHub.Application.TokenManagement.Interfaces;
using ControlHub.Application.TokenManagement.Interfaces.Generate;
using ControlHub.Application.TokenManagement.Interfaces.Repositories;
using ControlHub.Application.TokenManagement.Interfaces.Sender;
using ControlHub.Infrastructure.TokenManagement.Services;
using ControlHub.Infrastructure.TokenManagement.Services.Generate;
using ControlHub.Infrastructure.TokenManagement.Persistence.Repositories;
using ControlHub.Infrastructure.TokenManagement.Services.Sender;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace ControlHub.Infrastructure.DependencyInjection;

internal static class TokenManagementExtensions
{
    internal static IServiceCollection AddControlHubTokenManagement(
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
