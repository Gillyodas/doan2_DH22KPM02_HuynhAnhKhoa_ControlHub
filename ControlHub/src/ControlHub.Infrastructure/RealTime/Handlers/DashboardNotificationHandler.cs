using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ControlHub.Application.Common.Events;
using ControlHub.Infrastructure.RealTime.Services;
using MediatR;
using Microsoft.Extensions.Logging;

namespace ControlHub.Infrastructure.RealTime.Handlers
{
    public class DashboardNotificationHandler : INotificationHandler<LoginAttemptedEvent>
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

        public Task Handle(LoginAttemptedEvent notification, CancellationToken cancellationToken)
        {
            _buffer.Enqueue(notification);
            _logger.LogDebug("Enqueued login event to buffer. Success: {IsSuccess}", notification.IsSuccess);
            return Task.CompletedTask;
        }
    }
}
