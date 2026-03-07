using ControlHub.Application.Common.Settings;
using ControlHub.Application.Permissions.Interfaces.Repositories;
using ControlHub.Application.Roles.Interfaces.Repositories;
using ControlHub.Domain.AccessControl.Services;
using ControlHub.Infrastructure.Permissions.Repositories;
using ControlHub.Infrastructure.Roles.Repositories;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace ControlHub.Infrastructure.DependencyInjection;

internal static class RolePermissionExtensions
{
    internal static IServiceCollection AddControlHubRolePermissions(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.Configure<RoleSettings>(
            configuration.GetSection("RoleSettings"));

        // Role — Cached decorator pattern
        services.AddScoped<RoleRepository>();
        services.AddScoped<RoleQueries>();

        services.AddScoped<IRoleQueries>(sp =>
            new CachedRoleQueries(
                sp.GetRequiredService<RoleQueries>(),
                sp.GetRequiredService<IMemoryCache>(),
                sp.GetRequiredService<ILogger<CachedRoleQueries>>()));

        services.AddScoped<IRoleRepository>(sp =>
            new CachedRoleRepository(
                sp.GetRequiredService<RoleRepository>(),
                sp.GetRequiredService<IMemoryCache>(),
                sp.GetRequiredService<ILogger<CachedRoleRepository>>()));

        // Permission — Cached decorator pattern
        services.AddScoped<Infrastructure.Permissions.Repositories.PermissionRepository>();
        services.AddScoped<IPermissionRepository>(sp =>
            new CachedPermissionRepository(
                sp.GetRequiredService<Infrastructure.Permissions.Repositories.PermissionRepository>(),
                sp.GetRequiredService<IMemoryCache>()));

        services.AddScoped<IPermissionQueries, PermissionQueries>();

        // Domain Services
        services.AddScoped<CreateRoleWithPermissionsService>();
        services.AddScoped<AssignPermissionsService>();

        return services;
    }
}