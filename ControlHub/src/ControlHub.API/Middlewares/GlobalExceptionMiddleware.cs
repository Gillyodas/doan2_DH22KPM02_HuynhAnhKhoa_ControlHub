using System.Net;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

public class GlobalExceptionMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<GlobalExceptionMiddleware> _logger;
    private readonly IHostEnvironment _env;

    public GlobalExceptionMiddleware(RequestDelegate next, ILogger<GlobalExceptionMiddleware> logger, IHostEnvironment env)
    {
        _next = next;
        _logger = logger;
        _env = env;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            // Nếu response đã bắt đầu, không thể thay đổi headers/status nữa.
            if (context.Response.HasStarted)
            {
                _logger.LogWarning(ex, "Response already started, rethrowing. Path: {Path}, TraceId: {TraceId}",
                    context.Request.Path, context.TraceIdentifier);
                throw; // để ASP.NET hoặc hosting pipeline xử lý tiếp (vì đã bắt đầu gửi data)
            }

            _logger.LogError(ex, "Unhandled exception at {Path} with TraceId {TraceId}", context.Request.Path, context.TraceIdentifier);

            var (status, problemType, title) = MapExceptionToProblem(ex);

            var pd = new ProblemDetails
            {
                Type = problemType,
                Title = title,
                Status = status,
                Instance = context.Request.Path
            };

            // Trong dev hiển thị detail để debug, production thì che
            if (_env.IsDevelopment())
            {
                pd.Extensions["detail"] = ex.Message;
                pd.Extensions["stackTrace"] = ex.StackTrace;
            }
            else
            {
                pd.Extensions["traceId"] = context.TraceIdentifier;
            }

            context.Response.Clear();
            context.Response.StatusCode = status ?? (int)HttpStatusCode.InternalServerError;
            context.Response.ContentType = "application/problem+json";

            var options = new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };
            await context.Response.WriteAsJsonAsync(pd, options);
        }
    }

    private static (int? status, string type, string title) MapExceptionToProblem(Exception ex)
    {
        // Map các exception domain/infra thành status + problem type rõ ràng
        return ex switch
        {
            UnauthorizedAccessException _ => ((int)HttpStatusCode.Unauthorized, "https://httpstatuses.com/401", "Unauthorized"),
            KeyNotFoundException _ => ((int)HttpStatusCode.NotFound, "https://httpstatuses.com/404", "Not Found"),
            InvalidOperationException _ => ((int)HttpStatusCode.BadRequest, "https://httpstatuses.com/400", "Invalid Operation"),
            DbUpdateConcurrencyException _ => ((int)HttpStatusCode.Conflict, "urn:controlhub:errors:concurrency", "Concurrency error"),
            DbUpdateException _ => ((int)HttpStatusCode.InternalServerError, "urn:controlhub:errors:database", "Database error"),
            _ => ((int)HttpStatusCode.InternalServerError, "urn:controlhub:errors:unhandled", "Unhandled error")
        };
    }
}