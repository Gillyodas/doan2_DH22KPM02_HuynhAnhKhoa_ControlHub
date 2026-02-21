using System.Security.Claims;
using ControlHub.Application.Roles.Interfaces.Repositories;
using ControlHub.Application.Tokens; // Ch?a AppClaimTypes
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace ControlHub.Infrastructure.Authorization.Permissions
{
    internal class PermissionClaimsTransformation : IClaimsTransformation
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<PermissionClaimsTransformation> _logger;

        public PermissionClaimsTransformation(
            IServiceProvider serviceProvider,
            ILogger<PermissionClaimsTransformation> logger)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
        }

        public async Task<ClaimsPrincipal> TransformAsync(ClaimsPrincipal principal)
        {
            // 1. Ki?m tra x�c th?c co b?n
            if (principal.Identity?.IsAuthenticated != true)
            {
                // User chua dang nh?p, kh�ng c?n l�m g�
                return principal;
            }

            // 2. Ki?m tra xem d� transform chua (tr�nh ch?y l?p l?i)
            if (principal.HasClaim(c => c.Type == "Permission"))
            {
                return principal;
            }

            // 3. L?y RoleId t? Claim g?c
            var roleIdClaim = principal.FindFirst(AppClaimTypes.Role) ?? principal.FindFirst(ClaimTypes.Role);
            if (roleIdClaim == null || !Guid.TryParse(roleIdClaim.Value, out Guid roleId))
            {
                _logger.LogWarning("--- PermissionClaimsTransformation: Kh�ng t�m th?y RoleId h?p l? trong token. B? qua. ---");
                return principal;
            }

            // 4. Truy xu?t DB d? l?y Permissions
            using var scope = _serviceProvider.CreateScope();
            var roleRepository = scope.ServiceProvider.GetRequiredService<IRoleRepository>();

            var role = await roleRepository.GetByIdAsync(roleId, CancellationToken.None);

            // Case: Role kh�ng t?n t?i (d� b? x�a?)
            if (role == null)
            {
                _logger.LogWarning("--- PermissionClaimsTransformation: RoleId {RoleId} kh�ng t?n t?i trong DB. ---", roleId);
                return principal;
            }

            // Case: Role kh�ng c� quy?n n�o
            if (!role.Permissions.Any())
            {
                _logger.LogInformation("--- PermissionClaimsTransformation: Role {RoleName} kh�ng c� permission n�o. ---", role.Name);
                return principal;
            }

            // 5. Clone v� th�m Claims
            // Luu �: Ph?i Clone identity, kh�ng s?a tr?c ti?p tr�n principal g?c d? tr�nh side-effect
            var cloneIdentity = ((ClaimsIdentity)principal.Identity).Clone();

            var permissionCodes = new List<string>();

            foreach (var permission in role.Permissions)
            {
                cloneIdentity.AddClaim(new Claim("Permission", permission.Code));
                permissionCodes.Add(permission.Code);
            }

            // 6. Log k?t qu? (Gi?ng PermissionService cu)
            _logger.LogInformation(
                "--- PermissionClaimsTransformation: �� th�m {Count} quy?n cho Role {RoleName}. Danh s�ch: {Permissions} ---",
                role.Permissions.Count,
                role.Name,
                string.Join(", ", permissionCodes));

            return new ClaimsPrincipal(cloneIdentity);
        }
    }
}
