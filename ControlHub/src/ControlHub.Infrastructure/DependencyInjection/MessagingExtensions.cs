using ControlHub.Application.Messaging.Outbox;
using ControlHub.Application.Messaging.Outbox.Repositories;
using ControlHub.Infrastructure.Messaging.Outbox;
using ControlHub.Infrastructure.Messaging.Outbox.Handler;
using ControlHub.Infrastructure.Messaging.Outbox.Repositories;
using Microsoft.Extensions.DependencyInjection;

namespace ControlHub.Infrastructure.DependencyInjection;

internal static class MessagingExtensions
{
    internal static IServiceCollection AddControlHubMessaging(
        this IServiceCollection services)
    {
        services.AddScoped<IOutboxRepository, OutboxRepository>();
        services.AddScoped<OutboxHandlerFactory>();
        services.AddScoped<IOutboxHandler, EmailOutboxHandler>();
        services.AddHostedService<OutboxProcessor>();

        return services;
    }
}