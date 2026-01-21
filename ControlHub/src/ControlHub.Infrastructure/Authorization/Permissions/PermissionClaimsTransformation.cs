using System.Security.Claims;
using ControlHub.Application.Roles.Interfaces.Repositories;
using ControlHub.Application.Tokens; // Chứa AppClaimTypes
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
            // 1. Kiểm tra xác thực cơ bản
            if (principal.Identity?.IsAuthenticated != true)
            {
                // User chưa đăng nhập, không cần làm gì
                return principal;
            }

            // 2. Kiểm tra xem đã transform chưa (tránh chạy lặp lại)
            if (principal.HasClaim(c => c.Type == "Permission"))
            {
                return principal;
            }

            // 3. Lấy RoleId từ Claim gốc
            var roleIdClaim = principal.FindFirst(AppClaimTypes.Role) ?? principal.FindFirst(ClaimTypes.Role);
            if (roleIdClaim == null || !Guid.TryParse(roleIdClaim.Value, out Guid roleId))
            {
                _logger.LogWarning("--- PermissionClaimsTransformation: Không tìm thấy RoleId hợp lệ trong token. Bỏ qua. ---");
                return principal;
            }

            // 4. Truy xuất DB để lấy Permissions
            using var scope = _serviceProvider.CreateScope();
            var roleQueries = scope.ServiceProvider.GetRequiredService<IRoleQueries>();

            var role = await roleQueries.GetByIdAsync(roleId, CancellationToken.None);

            // Case: Role không tồn tại (đã bị xóa?)
            if (role == null)
            {
                _logger.LogWarning("--- PermissionClaimsTransformation: RoleId {RoleId} không tồn tại trong DB. ---", roleId);
                return principal;
            }

            // Case: Role không có quyền nào
            if (!role.Permissions.Any())
            {
                _logger.LogInformation("--- PermissionClaimsTransformation: Role {RoleName} không có permission nào. ---", role.Name);
                return principal;
            }

            // 5. Clone và thêm Claims
            // Lưu ý: Phải Clone identity, không sửa trực tiếp trên principal gốc để tránh side-effect
            var cloneIdentity = ((ClaimsIdentity)principal.Identity).Clone();

            var permissionCodes = new List<string>();

            foreach (var permission in role.Permissions)
            {
                cloneIdentity.AddClaim(new Claim("Permission", permission.Code));
                permissionCodes.Add(permission.Code);
            }

            // 6. Log kết quả (Giống PermissionService cũ)
            _logger.LogInformation(
                "--- PermissionClaimsTransformation: Đã thêm {Count} quyền cho Role {RoleName}. Danh sách: {Permissions} ---",
                role.Permissions.Count,
                role.Name,
                string.Join(", ", permissionCodes));

            return new ClaimsPrincipal(cloneIdentity);
        }
    }
}