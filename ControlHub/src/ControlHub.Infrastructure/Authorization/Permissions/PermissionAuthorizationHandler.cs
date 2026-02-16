using System.Security.Claims;
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
            Console.WriteLine($"[PermissionAuthorizationHandler] Checking permission: {requirement.Permission}");
            
            // Check if user is SuperAdmin (bypass all permission checks)
            var roleIdClaim = context.User.FindFirst(AppClaimTypes.Role) ?? context.User.FindFirst(ClaimTypes.Role);
            
            Console.WriteLine($"[PermissionAuthorizationHandler] Role claim found: {roleIdClaim?.Value ?? "NULL"}");
            Console.WriteLine($"[PermissionAuthorizationHandler] SuperAdmin ID: {ControlHubDefaults.Roles.SuperAdminId}");
            
            if (roleIdClaim != null && 
                roleIdClaim.Value.Equals(ControlHubDefaults.Roles.SuperAdminId.ToString(), StringComparison.OrdinalIgnoreCase))
            {
                Console.WriteLine($"[PermissionAuthorizationHandler] ? SuperAdmin detected! Bypassing permission check.");
                context.Succeed(requirement);
                return Task.CompletedTask;
            }

            // L?y t?t c? các claims 'Permission' t? user (dã du?c thêm t? IClaimsTransformation)
            var userPermissions = context.User.FindAll(AppClaimTypes.Permission);
            
            Console.WriteLine($"[PermissionAuthorizationHandler] User has {userPermissions.Count()} permission claims");

            // Ki?m tra xem user có claim nào kh?p v?i permission yêu c?u không
            if (userPermissions.Any(c => c.Value == requirement.Permission))
            {
                Console.WriteLine($"[PermissionAuthorizationHandler] ? Permission '{requirement.Permission}' found in user claims");
                // N?u có, dánh d?u là thành công
                context.Succeed(requirement);
            }
            else
            {
                Console.WriteLine($"[PermissionAuthorizationHandler] ? Permission '{requirement.Permission}' NOT found in user claims");
            }

            return Task.CompletedTask;
        }
    }

}
