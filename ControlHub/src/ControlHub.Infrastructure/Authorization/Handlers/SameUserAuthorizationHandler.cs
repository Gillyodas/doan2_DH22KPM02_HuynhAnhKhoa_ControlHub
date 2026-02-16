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
            // 1. L?y ID c?a ngu?i dang dang nh?p
            // Luu ý: ClaimTypes.NameIdentifier thu?ng map v?i 'sub' ho?c 'id' tùy config JWT c?a b?n
            var currentUserIdStr = context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                                   ?? context.User.FindFirst("sub")?.Value;

            if (Guid.TryParse(currentUserIdStr, out var currentUserId))
            {
                //Logic m? r?ng: Admin luôn du?c phép
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
