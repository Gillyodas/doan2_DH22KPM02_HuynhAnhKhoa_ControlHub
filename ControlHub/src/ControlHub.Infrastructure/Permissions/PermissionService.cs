using ControlHub.Application.Permissions.Interfaces;
using ControlHub.Application.Permissions.Interfaces.Repositories;
using Microsoft.Extensions.Logging;

namespace ControlHub.Infrastructure.Permissions
{
    public class PermissionService : IPermissionService
    {
        private readonly IPermissionQueries _permissionQueries;
        private readonly ILogger<PermissionService> _logger;
        public PermissionService(IPermissionQueries permissionQueries, ILogger<PermissionService> logger)
        {
            _permissionQueries = permissionQueries;
            _logger = logger;
        }
        public async Task<IEnumerable<string>> GetPermissionsForRoleIdAsync(Guid roleId, CancellationToken cancellationToken)
        {
            var permissions = await _permissionQueries.GetByRoleIdAsync(roleId, cancellationToken);
            _logger.LogInformation("Service get per: {Permissions}", string.Join(",", permissions.Select(p => p.Code)));

            return permissions.Select(p => p.Code).ToList();
        }
    }
}
