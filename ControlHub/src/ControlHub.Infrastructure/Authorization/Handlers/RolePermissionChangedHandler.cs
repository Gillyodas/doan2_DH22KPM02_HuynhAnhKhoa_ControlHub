using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ControlHub.Domain.AccessControl.Events;
using MediatR;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;

namespace ControlHub.Infrastructure.Authorization.Handlers
{
    /// <summary>
    /// Lắng nghe RolePermissionChangedEvent và invalidate cache tương ứng.
    /// Cache key format: "role-{roleId}" (khớp với CachedRoleRepository).
    /// Đảm bảo PermissionClaimsTransformation luôn lấy data mới nhất từ DB.
    /// </summary>
    internal class RolePermissionChangedHandler : INotificationHandler<RolePermissionChangedEvent>
    {
        private readonly IMemoryCache _cache;
        private readonly ILogger<RolePermissionChangedHandler> _logger;

        public RolePermissionChangedHandler(
            IMemoryCache cache,
            ILogger<RolePermissionChangedHandler> logger)
        {
            _cache = cache;
            _logger = logger;
        }

        public Task Handle(RolePermissionChangedEvent notification, CancellationToken cancellationToken)
        {
            var cacheKey = $"role-{notification.RoleId}";

            _cache.Remove(cacheKey);

            _logger.LogInformation(
                "Cache invalidated for role {RoleId} due to permission change. Key: {CacheKey}",
                notification.RoleId,
                cacheKey);

            return Task.CompletedTask;
        }
    }
}
