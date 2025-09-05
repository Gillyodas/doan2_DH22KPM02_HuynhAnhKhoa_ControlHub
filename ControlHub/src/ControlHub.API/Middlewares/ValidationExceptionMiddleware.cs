using ControlHub.SharedKernel.Accounts;

public class ValidationExceptionMiddleware
{
    private readonly RequestDelegate _next;

    public ValidationExceptionMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (FluentValidation.ValidationException ex)
        {
            var errors = ex.Errors
                .Select(e => new
                {
                    // map lại bằng cách lookup từ AccountErrors
                    Code = MapErrorCode(e.ErrorMessage),
                    Message = e.ErrorMessage
                });

            context.Response.StatusCode = StatusCodes.Status400BadRequest;
            context.Response.ContentType = "application/json";

            await context.Response.WriteAsJsonAsync(new { errors });
        }
    }

    private string MapErrorCode(string errorMessage)
    {
        // bạn có thể tạo dictionary <string, string> từ AccountErrors.Message → AccountErrors.Code
        return AccountErrorsCatalog.GetCodeByMessage(errorMessage);
    }
}
