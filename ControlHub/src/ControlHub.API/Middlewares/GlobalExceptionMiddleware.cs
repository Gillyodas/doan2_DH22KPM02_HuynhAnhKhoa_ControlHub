using System.Net;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;

namespace ControlHub.API.Middlewares
{
    public class GlobalExceptionMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<GlobalExceptionMiddleware> _logger;

        public GlobalExceptionMiddleware(RequestDelegate next, ILogger<GlobalExceptionMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            try
            {
                await _next(context);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Unhandled exception at {Path} with TraceId {TraceId}",
                    context.Request.Path,
                    context.TraceIdentifier);

                int statusCode = ex switch
                {
                    UnauthorizedAccessException => (int)HttpStatusCode.Unauthorized,
                    KeyNotFoundException => (int)HttpStatusCode.NotFound,
                    DbUpdateConcurrencyException => (int)HttpStatusCode.Conflict,
                    DbUpdateException => (int)HttpStatusCode.InternalServerError,
                    _ => (int)HttpStatusCode.InternalServerError
                };

                var errorResponse = new
                {
                    code = ex switch
                    {
                        DbUpdateConcurrencyException => "System.ConcurrencyError",
                        DbUpdateException => "System.DatabaseError",
                        _ => "System.UnhandledException"
                    },
                    message = "Unexpected error occurred",
                    traceId = context.TraceIdentifier
                };

                context.Response.StatusCode = statusCode;

                context.Response.ContentType = "application/json";
                await context.Response.WriteAsJsonAsync(errorResponse);
            }
        }
    }
}