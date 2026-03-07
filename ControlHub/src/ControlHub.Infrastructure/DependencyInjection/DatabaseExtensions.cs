using ControlHub.Application.Common.Persistence;
using ControlHub.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace ControlHub.Infrastructure.DependencyInjection;

internal static class DatabaseExtensions
{
    internal static IServiceCollection AddControlHubDatabase(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddDbContext<AppDbContext>(options =>
            options.UseSqlServer(
                configuration.GetConnectionString("DefaultConnection"),
                b =>
                {
                    b.MigrationsAssembly(typeof(AppDbContext).Assembly.FullName);
                    b.MigrationsHistoryTable("__EFMigrationsHistory", "ControlHub");
                })
            .ConfigureWarnings(w =>
                w.Ignore(RelationalEventId.CommandExecuted)));

        services.AddScoped<IUnitOfWork, UnitOfWork>();

        return services;
    }
}