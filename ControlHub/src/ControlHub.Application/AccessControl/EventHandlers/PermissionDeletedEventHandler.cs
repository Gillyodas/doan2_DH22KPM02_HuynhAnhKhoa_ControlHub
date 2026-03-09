using ControlHub.Application.AccessControl.Events;
using MediatR;
using Microsoft.Extensions.Logging;

namespace ControlHub.Application.AccessControl.EventHandlers;

internal sealed class PermissionDeletedEventHandler : INotificationHandler<PermissionDeletedEvent>
{
    private readonly ILogger<PermissionDeletedEventHandler> _logger;

    public PermissionDeletedEventHandler(ILogger<PermissionDeletedEventHandler> logger)
    {
        _logger = logger;
    }

    public Task Handle(PermissionDeletedEvent notification, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Permission deleted: {PermissionId}", notification.PermissionId);
        return Task.CompletedTask;
    }
}
