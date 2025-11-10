using System.Security.Claims;
using ControlHub.Application.Permissions.Interfaces;

namespace ControlHub.API.Middlewares
{
    public class PermissionMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<PermissionMiddleware> _logger;

        public PermissionMiddleware(RequestDelegate next, ILogger<PermissionMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            _logger.LogInformation("===> PermissionMiddleware TRIGGERED for {Path}", context.Request.Path);

            // Resolve scoped service tại runtime (đúng scope của request)
            var permissionService = context.RequestServices.GetRequiredService<IPermissionService>();

            // Nếu chưa login hoặc không có user => bỏ qua
            if (context.User?.Identity?.IsAuthenticated != true)
            {
                _logger.LogWarning("Bug o day");
                await _next(context);
                return;
            }

            // Lấy roleId hoặc userId từ token claims
            var roleIdClaim = context.User.FindFirst(ClaimTypes.Role);
            if (roleIdClaim == null)
            {
                _logger.LogWarning("Không tìm thấy claim Role trong token!");
                await _next(context);
                return;
            }

            _logger.LogInformation("User Claims: {@Claims}",
    context.User.Claims.Select(c => new { c.Type, c.Value }));

            var roleId = Guid.Parse(roleIdClaim.Value);

            var permissions = await permissionService.GetPermissionsForRoleIdAsync(roleId, context.RequestAborted);

            if (permissions == null)
            {
                await _next(context);
                return;
            }

            var newIdentity = new ClaimsIdentity(context.User.Identity);
            foreach (var per in permissions)
            {
                newIdentity.AddClaim(new Claim("permission", per.ToLowerInvariant()));
            }
            context.User = new ClaimsPrincipal(newIdentity);

            _logger.LogInformation("Claims sau middleware: {@Claims}",
    context.User.Claims.Select(c => new { c.Type, c.Value }));

            await _next(context);
        }
    }
}