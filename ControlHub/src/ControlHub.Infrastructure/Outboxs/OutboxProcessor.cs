using ControlHub.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace ControlHub.Infrastructure.Outboxs
{
    public class OutboxProcessor : BackgroundService
    {
        private readonly IServiceProvider _services;
        private readonly ILogger<OutboxProcessor> _logger;

        public OutboxProcessor(IServiceProvider services, ILogger<OutboxProcessor> logger)
        {
            _services = services;
            _logger = logger;
        }
        // TODO: V?n d?: Failed messages ch? du?c mark failed, không có retry logic - M?c d?: Minor - Feature gap - Impact: Messages fail s? không du?c x? lý l?i t? d?ng
        protected override async Task ExecuteAsync(CancellationToken cancellationToken)
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                using var scope = _services.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                var handlerFactory = scope.ServiceProvider.GetRequiredService<OutboxHandlerFactory>();

                var messages = await db.OutboxMessages
                    .Where(m => !m.Processed)
                    .OrderBy(m => m.OccurredOn)
                    .Take(20)
                    .ToListAsync(cancellationToken);

                if (messages.Any())
                {
                    foreach (var msg in messages)
                    {
                        try
                        {
                            var handler = handlerFactory.Get(msg.Type);
                            if (handler == null)
                            {
                                continue;
                            }

                            await handler.HandleAsync(msg.Payload, cancellationToken);

                            msg.MarkProcessed();
                        }
                        catch (Exception ex)
                        {
                            msg.MarkFailed(ex.Message);
                            _logger.LogError(ex, "Failed to process outbox {Id}", msg.Id);
                        }
                    }

                    await db.SaveChangesAsync(cancellationToken);
                }

                await Task.Delay(5000, cancellationToken);
            }
        }

        private record EmailPayload(string To, string Subject, string Body);
    }
}
