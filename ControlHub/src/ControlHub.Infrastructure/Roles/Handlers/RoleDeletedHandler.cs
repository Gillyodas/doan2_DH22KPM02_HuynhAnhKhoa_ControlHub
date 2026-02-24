using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ControlHub.Domain.AccessControl.Events;
using MediatR;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;

namespace ControlHub.Infrastructure.Roles.Handlers
{
    internal class RoleDeletedHandler : INotificationHandler<RoleDeletedEvent>
    {
        private readonly IMemoryCache _cache;
        private readonly ILogger<RoleDeletedHandler> _logger;

        public RoleDeletedHandler(IMemoryCache cache, ILogger<RoleDeletedHandler> logger)
        {
            _cache = cache;
            _logger = logger;
        }
        public Task Handle(RoleDeletedEvent notification, CancellationToken cancellationToken)
        {
            var cacheKey = $"role-{notification.RoleId}";

            _cache.Remove(cacheKey);

            _logger.LogInformation(
                "Cache invalidated for role {RoleId} due to role delete. Key: {CacheKey}",
                notification.RoleId,
                cacheKey);

            return Task.CompletedTask;
        }
    }
}
