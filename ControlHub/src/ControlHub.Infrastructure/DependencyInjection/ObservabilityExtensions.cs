using ControlHub.Application.Common.Logging.Interfaces;
using ControlHub.Infrastructure.Logging;
using ControlHub.Infrastructure.RealTime.Services;
using Microsoft.Extensions.DependencyInjection;

namespace ControlHub.Infrastructure.DependencyInjection;

internal static class ObservabilityExtensions
{
    internal static IServiceCollection AddControlHubObservability(
        this IServiceCollection services)
    {
        // Logging
        services.AddScoped<ILogReaderService, LogReaderService>();

        // SignalR & Real-time
        services.AddSignalR();
        services.AddSingleton<IActiveUserTracker, InMemoryActiveUserTracker>();
        services.AddSingleton<LoginEventBuffer>();
        services.AddHostedService(sp => sp.GetRequiredService<LoginEventBuffer>());

        return services;
    }
}