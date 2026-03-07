using ControlHub.Infrastructure.Accounts.Security;
using ControlHub.Infrastructure.Authorization.Handlers;
using ControlHub.Infrastructure.Authorization.Permissions;
using ControlHub.Infrastructure.Permissions.AuthZ;
using ControlHub.Infrastructure.Tokens;
using ControlHub.Domain.Identity.Security;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace ControlHub.Infrastructure.DependencyInjection;

internal static class SecurityExtensions
{
    internal static IServiceCollection AddControlHubSecurity(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // Hashing
        services.AddScoped<IPasswordHasher, Argon2PasswordHasher>();

        // JWT Authentication
        services.ConfigureOptions<ConfigureJwtBearerOptions>();
        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer();

        // Authorization
        services.AddTransient<IClaimsTransformation, PermissionClaimsTransformation>();
        services.AddSingleton<IAuthorizationPolicyProvider, PermissionPolicyProvider>();
        services.AddSingleton<IAuthorizationHandler, PermissionAuthorizationHandler>();
        services.AddSingleton<IAuthorizationHandler, SameUserAuthorizationHandler>();

        services.AddAuthorization(options =>
        {
            options.AddPolicy("DashboardAccess", policy =>
                policy.RequireAssertion(context =>
                {
                    var roleClaim = context.User.FindFirst("role")?.Value;
                    return roleClaim == "Admin" || roleClaim == "SupperAdmin";
                }));
        });

        return services;
    }
}