using ControlHub.Application.Accounts.Interfaces;
using ControlHub.Application.Accounts.Interfaces.Repositories;
using ControlHub.Application.Common.Interfaces;
using ControlHub.Application.Users.Interfaces.Repositories;
using ControlHub.Application.Emails.Interfaces;
using ControlHub.Domain.Identity.Identifiers.Rules;
using ControlHub.Domain.Identity.Identifiers.Services;
using ControlHub.Infrastructure.Accounts.Factories;
using ControlHub.Infrastructure.Accounts.Repositories;
using ControlHub.Infrastructure.Accounts.Validators;
using ControlHub.Infrastructure.Emails;
using ControlHub.Infrastructure.Services;
using ControlHub.Infrastructure.Users.Repositories;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;

namespace ControlHub.Infrastructure.DependencyInjection;

internal static class AccountExtensions
{
    internal static IServiceCollection AddControlHubAccounts(
        this IServiceCollection services)
    {
        // Email
        services.AddScoped<IEmailSender, SmtpEmailSender>();

        // Account & User
        services.AddScoped<IAccountValidator, AccountValidator>();
        services.AddScoped<IAccountFactory, AccountFactory>();
        services.AddScoped<IAccountQueries, AccountQueries>();
        services.AddScoped<IAccountRepository, AccountRepository>();
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IUserQueries, UserQueries>();
        services.AddScoped<ICurrentUserService, CurrentUserService>();
        services.AddHttpContextAccessor();

        // Identifier — Cached decorator pattern
        services.AddScoped<Infrastructure.Accounts.Repositories.IdentifierConfigRepository>();
        services.AddScoped<Application.Accounts.Interfaces.Repositories.IIdentifierConfigRepository>(sp =>
            new CachedIdentifierConfigRepository(
                sp.GetRequiredService<Infrastructure.Accounts.Repositories.IdentifierConfigRepository>(),
                sp.GetRequiredService<IMemoryCache>()));

        services.AddScoped<Domain.Identity.Identifiers.IIdentifierConfigRepository>(sp =>
            sp.GetRequiredService<Application.Accounts.Interfaces.Repositories.IIdentifierConfigRepository>());

        // Domain Services
        services.AddScoped<DynamicIdentifierValidator>();
        services.AddScoped<IdentifierFactory>();
        services.AddScoped<IIdentifierValidator, EmailIdentifierValidator>();
        services.AddScoped<IIdentifierValidator, UsernameIdentifierValidator>();
        services.AddScoped<IIdentifierValidator, PhoneIdentifierValidator>();

        return services;
    }
}