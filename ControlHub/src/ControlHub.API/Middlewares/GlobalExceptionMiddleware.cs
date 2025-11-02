using System.Net;
using System.Text.Json;
using ControlHub.SharedKernel.Common.Exceptions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

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
            if (context.Response.HasStarted)
            {
                _logger.LogWarning(ex, "Response already started, rethrowing. Path: {Path}, TraceId: {TraceId}",
                    context.Request.Path, context.TraceIdentifier);
                throw;
            }

            var (status, type, title) = MapExceptionToProblem(ex);

            _logger.LogError(ex,
                "Unhandled exception at {Path} [{Type}] with TraceId {TraceId}",
                context.Request.Path, ex.GetType().Name, context.TraceIdentifier);

            var pd = new ProblemDetails
            {
                Type = type,
                Title = title,
                Status = status,
                Instance = context.Request.Path
            };

            if (_env.IsDevelopment())
            {
                pd.Extensions["detail"] = ex.Message;
                pd.Extensions["stackTrace"] = ex.StackTrace;
                if (ex.InnerException != null)
                    pd.Extensions["inner"] = ex.InnerException.Message;
            }
            else
            {
                pd.Extensions["traceId"] = context.TraceIdentifier;
            }

            context.Response.Clear();
            context.Response.StatusCode = status;
            context.Response.ContentType = "application/problem+json";

            await context.Response.WriteAsJsonAsync(pd, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            });
        }
    }

    private static (int status, string type, string title) MapExceptionToProblem(Exception ex)
    {
        return ex switch
        {
            UnauthorizedAccessException _ => ((int)HttpStatusCode.Unauthorized, "https://httpstatuses.com/401", "Unauthorized"),
            KeyNotFoundException _ => ((int)HttpStatusCode.NotFound, "https://httpstatuses.com/404", "Not Found"),
            InvalidOperationException _ => ((int)HttpStatusCode.BadRequest, "https://httpstatuses.com/400", "Invalid Operation"),
            DbUpdateConcurrencyException _ => ((int)HttpStatusCode.Conflict, "urn:controlhub:errors:concurrency", "Concurrency error"),
            ApplicationException _ => ((int)HttpStatusCode.BadRequest, "urn:controlhub:errors:application", "Application layer error"),
            RepositoryConcurrencyException _ => ((int)HttpStatusCode.Conflict, "urn:controlhub:errors:repository-concurrency", "Repository concurrency error"),
            RepositoryException _ => ((int)HttpStatusCode.InternalServerError, "urn:controlhub:errors:repository", "Database repository error"),
            DbUpdateException _ => ((int)HttpStatusCode.InternalServerError, "urn:controlhub:errors:database", "Database error"),
            _ => ((int)HttpStatusCode.InternalServerError, "urn:controlhub:errors:unhandled", "Unhandled error")
        };
    }
}