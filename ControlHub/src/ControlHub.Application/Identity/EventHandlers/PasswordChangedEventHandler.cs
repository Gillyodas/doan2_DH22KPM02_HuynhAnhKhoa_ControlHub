using ControlHub.Application.Identity.Events;
using MediatR;
using Microsoft.Extensions.Logging;

namespace ControlHub.Application.Identity.EventHandlers
{
    public class PasswordChangedEventHandler : INotificationHandler<PasswordChangedEvent>
    {
        private readonly ILogger<PasswordChangedEventHandler> _logger;

        public PasswordChangedEventHandler(ILogger<PasswordChangedEventHandler> logger)
        {
            _logger = logger;
        }

        public Task Handle(PasswordChangedEvent notification, CancellationToken cancellationToken)
        {
            _logger.LogInformation(
                "PasswordChanged | AccountId: {AccountId} | Timestamp: {Timestamp}",
                notification.AccountId,
                notification.Timestamp);

            return Task.CompletedTask;
        }
    }
}
