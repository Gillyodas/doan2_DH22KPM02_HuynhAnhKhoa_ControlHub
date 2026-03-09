using System.Security.Claims;
using ControlHub.Application.AccessControl.Authorization;
using ControlHub.Application.Common.Security;
using ControlHub.SharedKernel.Constants;
using Microsoft.AspNetCore.Authorization;

namespace ControlHub.Infrastructure.AccessControl.Authorization.Handlers
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

            // L?y t?t c? c�c claims 'Permission' t? user (d� du?c th�m t? IClaimsTransformation)
            var userPermissions = context.User.FindAll(AppClaimTypes.Permission);

            Console.WriteLine($"[PermissionAuthorizationHandler] User has {userPermissions.Count()} permission claims");

            // Ki?m tra xem user c� claim n�o kh?p v?i permission y�u c?u kh�ng
            if (userPermissions.Any(c => c.Value == requirement.Permission))
            {
                Console.WriteLine($"[PermissionAuthorizationHandler] ? Permission '{requirement.Permission}' found in user claims");
                // N?u c�, d�nh d?u l� th�nh c�ng
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
