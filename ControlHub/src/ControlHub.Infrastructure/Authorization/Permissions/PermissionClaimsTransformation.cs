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
            // 1. Ki?m tra xác th?c co b?n
            if (principal.Identity?.IsAuthenticated != true)
            {
                // User chua dang nh?p, không c?n làm gì
                return principal;
            }

            // 2. Ki?m tra xem dã transform chua (tránh ch?y l?p l?i)
            if (principal.HasClaim(c => c.Type == "Permission"))
            {
                return principal;
            }

            // 3. L?y RoleId t? Claim g?c
            var roleIdClaim = principal.FindFirst(AppClaimTypes.Role) ?? principal.FindFirst(ClaimTypes.Role);
            if (roleIdClaim == null || !Guid.TryParse(roleIdClaim.Value, out Guid roleId))
            {
                _logger.LogWarning("--- PermissionClaimsTransformation: Không tìm th?y RoleId h?p l? trong token. B? qua. ---");
                return principal;
            }

            // 4. Truy xu?t DB d? l?y Permissions
            using var scope = _serviceProvider.CreateScope();
            var roleQueries = scope.ServiceProvider.GetRequiredService<IRoleQueries>();

            var role = await roleQueries.GetByIdAsync(roleId, CancellationToken.None);

            // Case: Role không t?n t?i (dã b? xóa?)
            if (role == null)
            {
                _logger.LogWarning("--- PermissionClaimsTransformation: RoleId {RoleId} không t?n t?i trong DB. ---", roleId);
                return principal;
            }

            // Case: Role không có quy?n nào
            if (!role.Permissions.Any())
            {
                _logger.LogInformation("--- PermissionClaimsTransformation: Role {RoleName} không có permission nào. ---", role.Name);
                return principal;
            }

            // 5. Clone và thêm Claims
            // Luu ý: Ph?i Clone identity, không s?a tr?c ti?p trên principal g?c d? tránh side-effect
            var cloneIdentity = ((ClaimsIdentity)principal.Identity).Clone();

            var permissionCodes = new List<string>();

            foreach (var permission in role.Permissions)
            {
                cloneIdentity.AddClaim(new Claim("Permission", permission.Code));
                permissionCodes.Add(permission.Code);
            }

            // 6. Log k?t qu? (Gi?ng PermissionService cu)
            _logger.LogInformation(
                "--- PermissionClaimsTransformation: Ðã thêm {Count} quy?n cho Role {RoleName}. Danh sách: {Permissions} ---",
                role.Permissions.Count,
                role.Name,
                string.Join(", ", permissionCodes));

            return new ClaimsPrincipal(cloneIdentity);
        }
    }
}
