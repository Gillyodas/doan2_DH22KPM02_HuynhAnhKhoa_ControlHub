using ControlHub.Application.Identity.Events;
using MediatR;
using Microsoft.Extensions.Logging;

namespace ControlHub.Application.Identity.EventHandlers
{
    public class AccountSignedInEventHandler : INotificationHandler<AccountSignedInEvent>
    {
        private readonly ILogger<AccountSignedInEventHandler> _logger;

        public AccountSignedInEventHandler(ILogger<AccountSignedInEventHandler> logger)
        {
            _logger = logger;
        }

        public Task Handle(AccountSignedInEvent notification, CancellationToken cancellationToken)
        {
            _logger.LogInformation(
                "AccountSignedIn | Success: {IsSuccess} | Identifier: {Identifier} | Reason: {Reason}",
                notification.IsSuccess,
                notification.MaskedIdentifier,
                notification.FailureReason);

            return Task.CompletedTask;
        }
    }
}
