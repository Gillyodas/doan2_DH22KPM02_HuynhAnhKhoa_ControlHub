using ControlHub.Application.AccessControl.Interfaces.Repositories;
using ControlHub.Application.AccessControl.Settings;
using ControlHub.Domain.AccessControl.Services;
using ControlHub.Infrastructure.AccessControl.Persistence.Repositories;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace ControlHub.Infrastructure.DependencyInjection;

internal static class AccessControlExtensions
{
    internal static IServiceCollection AddControlHubAccessControl(
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
        services.AddScoped<Infrastructure.AccessControl.Persistence.Repositories.PermissionRepository>();
        services.AddScoped<IPermissionRepository>(sp =>
            new CachedPermissionRepository(
                sp.GetRequiredService<Infrastructure.AccessControl.Persistence.Repositories.PermissionRepository>(),
                sp.GetRequiredService<IMemoryCache>()));

        services.AddScoped<IPermissionQueries, PermissionQueries>();

        // Domain Services
        services.AddScoped<CreateRoleWithPermissionsService>();
        services.AddScoped<AssignPermissionsService>();

        return services;
    }
}
