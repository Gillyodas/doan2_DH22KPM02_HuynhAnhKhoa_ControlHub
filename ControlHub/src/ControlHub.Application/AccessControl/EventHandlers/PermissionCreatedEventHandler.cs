using ControlHub.Application.AccessControl.Events;
using MediatR;
using Microsoft.Extensions.Logging;

namespace ControlHub.Application.AccessControl.EventHandlers;

internal sealed class PermissionCreatedEventHandler : INotificationHandler<PermissionCreatedEvent>
{
    private readonly ILogger<PermissionCreatedEventHandler> _logger;

    public PermissionCreatedEventHandler(ILogger<PermissionCreatedEventHandler> logger)
    {
        _logger = logger;
    }

    public Task Handle(PermissionCreatedEvent notification, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Permission created: {PermissionId}", notification.PermissionId);
        return Task.CompletedTask;
    }
}
