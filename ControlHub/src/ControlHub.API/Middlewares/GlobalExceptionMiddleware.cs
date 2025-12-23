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

            var (status, type, title, code) = MapExceptionToProblem(ex);

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

            pd.Extensions["code"] = code;

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

    private static (int status, string type, string title, string code) MapExceptionToProblem(Exception ex)
    {
        return ex switch
        {
            UnauthorizedAccessException _ => ((int)HttpStatusCode.Unauthorized, "https://httpstatuses.com/401", "Unauthorized", "Auth.Unauthorized"),
            KeyNotFoundException _ => ((int)HttpStatusCode.NotFound, "https://httpstatuses.com/404", "Not Found", "Common.NotFound"),
            InvalidOperationException _ => ((int)HttpStatusCode.BadRequest, "https://httpstatuses.com/400", "Invalid Operation", "Common.InvalidOperation"),
            DbUpdateConcurrencyException _ => ((int)HttpStatusCode.Conflict, "urn:controlhub:errors:concurrency", "Concurrency error", "Database.Concurrency"),
            ApplicationException _ => ((int)HttpStatusCode.BadRequest, "urn:controlhub:errors:application", "Application layer error", "Application.Error"),
            RepositoryConcurrencyException _ => ((int)HttpStatusCode.Conflict, "urn:controlhub:errors:repository-concurrency", "Repository concurrency error", "Repository.Concurrency"),
            RepositoryException _ => ((int)HttpStatusCode.InternalServerError, "urn:controlhub:errors:repository", "Database repository error", "Repository.Error"),
            DbUpdateException _ => ((int)HttpStatusCode.InternalServerError, "urn:controlhub:errors:database", "Database error", "Database.Error"),
            _ => ((int)HttpStatusCode.InternalServerError, "urn:controlhub:errors:unhandled", "Unhandled error", "Unhandled.Error")
        };
    }
}