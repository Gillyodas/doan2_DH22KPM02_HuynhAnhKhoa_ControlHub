using System.Security.Claims;
using ControlHub.Application.Permissions.Interfaces;
using ControlHub.Application.Tokens;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Logging;

namespace ControlHub.Infrastructure.Permissions.AuthZ
{
    public class PermissionClaimsTransformation : IClaimsTransformation
    {
        private readonly IPermissionService _permissionService;
        private readonly ILogger<PermissionClaimsTransformation> _logger;

        public PermissionClaimsTransformation(
            IPermissionService permissionService,
            ILogger<PermissionClaimsTransformation> logger)
        {
            _permissionService = permissionService;
            _logger = logger;
        }

        public async Task<ClaimsPrincipal> TransformAsync(ClaimsPrincipal principal)
        {
            // *** LOG CHỦ ĐỘNG ***
            _logger.LogWarning("--- PermissionClaimsTransformation ĐANG CHẠY ---");

            if (principal.Identity?.IsAuthenticated != true)
            {
                _logger.LogWarning("--- User chưa được xác thực, bỏ qua Transform-- - ");
                return principal;
            }

            var roleIdClaim = principal.FindFirst(AppClaimTypes.Role);
            if (roleIdClaim == null || !Guid.TryParse(roleIdClaim.Value, out Guid roleId))
            {
                _logger.LogWarning("--- Không tìm thấy RoleId claim, bỏ qua Transform ---");
                return principal;
            }

            _logger.LogInformation("--- Đang lấy permission cho RoleId: {RoleId} ---", roleId);
            var permissions = await _permissionService.GetPermissionsForRoleIdAsync(roleId, CancellationToken.None);

            if (!permissions.Any())
            {
                _logger.LogWarning("--- RoleId: {RoleId} không có permission nào, bỏ qua Transform ---", roleId);
                return principal;
            }

            var permissionsIdentity = new ClaimsIdentity();
            foreach (var permissionName in permissions)
            {
                permissionsIdentity.AddClaim(new System.Security.Claims.Claim(AppClaimTypes.Permission, permissionName));
            }

            principal.AddIdentity(permissionsIdentity);

            _logger.LogInformation("--- ĐÃ THÊM {Count} PERMISSIONS CHO USER ---", permissions.Count());
            return principal;
        }
    }
}