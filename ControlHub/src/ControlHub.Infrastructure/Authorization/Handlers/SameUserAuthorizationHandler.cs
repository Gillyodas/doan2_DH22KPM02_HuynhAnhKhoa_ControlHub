using System.Security.Claims;
using ControlHub.Application.Authorization.Requirements;
using Microsoft.AspNetCore.Authorization;

namespace ControlHub.Infrastructure.Authorization.Handlers
{
    internal class SameUserAuthorizationHandler : AuthorizationHandler<SameUserRequirement, Guid>
    {
        protected override Task HandleRequirementAsync(
            AuthorizationHandlerContext context,
            SameUserRequirement requirement,
            Guid resourceId)
        {
            // 1. Lấy ID của người đang đăng nhập
            // Lưu ý: ClaimTypes.NameIdentifier thường map với 'sub' hoặc 'id' tùy config JWT của bạn
            var currentUserIdStr = context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                                   ?? context.User.FindFirst("sub")?.Value;

            if (Guid.TryParse(currentUserIdStr, out var currentUserId))
            {
                //Logic mở rộng: Admin luôn được phép
                if (context.User.IsInRole("supper_admin"))
                {
                    context.Succeed(requirement);
                    return Task.CompletedTask;
                }

                if (currentUserId == resourceId)
                {
                    context.Succeed(requirement);
                }
            }

            return Task.CompletedTask;
        }
    }
}