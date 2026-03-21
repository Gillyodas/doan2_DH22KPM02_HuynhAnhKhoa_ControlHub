using System.Security.Claims;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.RateLimiting;

namespace ControlHub.API.Extensions
{
    public static class RateLimitingExtensions
    {
        public static class Policies
        {
            public const string Authentication = "auth-policy";      // Sign-in, register
            public const string Sensitive = "sensitive-policy";      // Reset password, verify email
            public const string GeneralApi = "general-api-policy";   // Các endpoint còn lại
        }

        public static IServiceCollection AddControlHubRateLimiting(
            this IServiceCollection services,
            IConfiguration configuration)
        {
            services.AddRateLimiter(options =>
            {
                options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

                // Policy 1: Authentication endpoints (login, register)
                // Sliding Window: 5 requests / 15 phút / per IP
                options.AddSlidingWindowLimiter(Policies.Authentication, opt =>
                {
                    opt.Window = TimeSpan.FromMinutes(15);
                    opt.SegmentsPerWindow = 3;
                    opt.PermitLimit = 5000;
                    opt.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
                    opt.QueueLimit = 0;
                });

                // Policy 2: Sensitive endpoints (reset password, verify email)
                // Fixed Window: 3 requests / 1 giờ / per IP
                options.AddFixedWindowLimiter(Policies.Sensitive, opt =>
                {
                    opt.Window = TimeSpan.FromHours(1);
                    opt.PermitLimit = 3000;
                    opt.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
                    opt.QueueLimit = 0;
                });

                // Policy 3: General API
                // Fixed Window: 20 requests / 1 phút / per IP, 200 requests / 1 phút / per id
                options.AddPolicy(Policies.GeneralApi, context =>
                {
                    var userId = context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                    if (userId != null)
                    {
                        return RateLimitPartition.GetFixedWindowLimiter(userId, _ => new()
                        {
                            PermitLimit = 20000,
                            Window = TimeSpan.FromMinutes(1)
                        });
                    }

                    var ip = context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
                    return RateLimitPartition.GetFixedWindowLimiter(ip, _ => new()
                    {
                        PermitLimit = 2000,
                        Window = TimeSpan.FromMinutes(1)
                    });
                });

                options.OnRejected = async (context, ct) =>
                {
                    context.HttpContext.Response.StatusCode = StatusCodes.Status429TooManyRequests;

                    var logger = context.HttpContext.RequestServices
                        .GetRequiredService<ILoggerFactory>()
                        .CreateLogger("RateLimiting");
                    logger.LogWarning("Rate limit exceeded for {IP} on {Path}",
                        context.HttpContext.Connection.RemoteIpAddress,
                        context.HttpContext.Request.Path);

                    if (context.Lease.TryGetMetadata(MetadataName.RetryAfter, out var retryAfter))
                    {
                        context.HttpContext.Response.Headers.RetryAfter = ((int)retryAfter.TotalSeconds).ToString();
                    }

                    context.HttpContext.Response.ContentType = "application/problem+json";
                    await context.HttpContext.Response.WriteAsync("""
                    {
                        "type": "https://tools.ietf.org/html/rfc6585#section-4",
                        "title": "Too Many Requests",
                        "status": 429,
                        "detail": "You have exceeded the request limit. Please try again later."
                    }
                    """, ct);
                };
            });

            return services;
        }
    }
}
