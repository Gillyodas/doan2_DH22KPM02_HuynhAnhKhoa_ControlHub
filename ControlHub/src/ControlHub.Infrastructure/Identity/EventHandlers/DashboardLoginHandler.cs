using ControlHub.Application.Identity.Events;
using ControlHub.Infrastructure.Identity.Services;
using MediatR;
using Microsoft.Extensions.Logging;

namespace ControlHub.Infrastructure.Identity.EventHandlers
{
    internal class DashboardLoginHandler : INotificationHandler<AccountSignedInEvent>
    {
        private readonly LoginEventBuffer _buffer;
        private readonly ILogger<DashboardLoginHandler> _logger;

        public DashboardLoginHandler(LoginEventBuffer buffer, ILogger<DashboardLoginHandler> logger)
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
