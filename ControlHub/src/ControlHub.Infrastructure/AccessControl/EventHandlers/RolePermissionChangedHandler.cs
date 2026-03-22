using ControlHub.Domain.AccessControl.Events;
using MediatR;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;

namespace ControlHub.Infrastructure.AccessControl.EventHandlers
{
    internal class RolePermissionChangedHandler : INotificationHandler<RolePermissionChangedEvent>
    {
        private readonly IMemoryCache _cache;
        private readonly ILogger<RolePermissionChangedHandler> _logger;

        public RolePermissionChangedHandler(IMemoryCache cache, ILogger<RolePermissionChangedHandler> logger)
        {
            _cache = cache;
            _logger = logger;
        }

        public Task Handle(RolePermissionChangedEvent notification, CancellationToken cancellationToken)
        {
            _cache.Remove($"role-{notification.RoleId}");
            _cache.Remove($"role-perms-{notification.RoleId}");

            _logger.LogInformation(
                "RolePermissionChangedEvent: invalidated cache for role-{RoleId}, role-perms-{RoleId}",
                notification.RoleId,
                notification.RoleId);

            return Task.CompletedTask;
        }
    }
}
