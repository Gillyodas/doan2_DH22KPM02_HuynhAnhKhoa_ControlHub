using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ControlHub.Application.Roles.Interfaces.Repositories;
using ControlHub.Domain.AccessControl.Events;
using MediatR;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;

namespace ControlHub.Infrastructure.Roles.Handlers
{
    internal class RoleCreatedHandler : INotificationHandler<RoleCreatedEvent>
    {
        private readonly IMemoryCache _cache;
        private readonly IRoleQueries _roleQueries;
        private readonly ILogger<RoleCreatedHandler> _logger;

        public RoleCreatedHandler(IMemoryCache cache, IRoleQueries roleQueries, ILogger<RoleCreatedHandler> logger)
        {
            _cache = cache;
            _roleQueries = roleQueries;
            _logger = logger;
        }
        public Task Handle(RoleCreatedEvent notification, CancellationToken cancellationToken)
        {
            _roleQueries.GetByIdAsync(notification.RoleId, cancellationToken);

            _logger.LogInformation(
                "RoleCreatedEvent received: RoleId={RoleId}",
                notification.RoleId);

            return Task.CompletedTask;
        }
    }
}
