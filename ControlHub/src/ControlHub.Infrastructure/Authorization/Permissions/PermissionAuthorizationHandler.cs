using ControlHub.Application.Tokens;
using ControlHub.Application.Authorization.Requirements;
using ControlHub.SharedKernel.Constants;
using Microsoft.AspNetCore.Authorization;

namespace ControlHub.Infrastructure.Permissions.AuthZ
{
    internal class PermissionAuthorizationHandler : AuthorizationHandler<PermissionRequirement>
    {
        protected override Task HandleRequirementAsync(
            AuthorizationHandlerContext context,
            PermissionRequirement requirement)
        {
            // Lấy tất cả các claims 'Permission' từ user (đã được thêm từ IClaimsTransformation)
            var userPermissions = context.User.FindAll(AppClaimTypes.Permission);

            if (context.User.IsInRole(ControlHubDefaults.Roles.SuperAdminId.ToString()))
            {
                context.Succeed(requirement);
                return Task.CompletedTask;
            }

            // Kiểm tra xem user có claim nào khớp với permission yêu cầu không
            if (userPermissions.Any(c => c.Value == requirement.Permission))
            {
                // Nếu có, đánh dấu là thành công
                context.Succeed(requirement);
            }

            return Task.CompletedTask;
        }
    }
}
