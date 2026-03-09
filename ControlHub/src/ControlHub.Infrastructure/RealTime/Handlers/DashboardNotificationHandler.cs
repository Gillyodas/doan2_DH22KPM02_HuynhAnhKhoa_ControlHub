using ControlHub.Application.Identity.Events;
using ControlHub.Infrastructure.RealTime.Services;
using MediatR;
using Microsoft.Extensions.Logging;

namespace ControlHub.Infrastructure.RealTime.Handlers
{
    public class DashboardNotificationHandler : INotificationHandler<AccountSignedInEvent>
    {
        private readonly LoginEventBuffer _buffer;
        private readonly ILogger<DashboardNotificationHandler> _logger;

        public DashboardNotificationHandler(
        LoginEventBuffer buffer,
        ILogger<DashboardNotificationHandler> logger)
        {
            _buffer = buffer;
            _logger = logger;
        }

        public Task Handle(AccountSignedInEvent notification, CancellationToken cancellationToken)
        {
            _buffer.Enqueue(notification);
            _logger.LogDebug("Enqueued login event to buffer. Success: {IsSuccess}", notification.IsSuccess);
            return Task.CompletedTask;
        }
    }
}
