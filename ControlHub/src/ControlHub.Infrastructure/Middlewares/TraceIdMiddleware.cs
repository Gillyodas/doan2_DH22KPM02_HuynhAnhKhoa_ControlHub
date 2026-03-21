using Microsoft.AspNetCore.Http;

namespace ControlHub.Infrastructure.Middlewares;

public class TraceIdMiddleware
{
    private readonly RequestDelegate _next;

    public TraceIdMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        // Gắn TraceId vào response header để client biết được TraceId khi cần báo lỗi
        context.Response.OnStarting(() =>
        {
            context.Response.Headers["X-Trace-Id"] = context.TraceIdentifier;
            return Task.CompletedTask;
        });

        await _next(context);
    }
}
