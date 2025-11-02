namespace ControlHub.SharedKernel.Common.Errors
{
    public sealed record Error(string Code, string Message)
    {
        public static readonly Error None = new(string.Empty, string.Empty);

        // Validation error
        public static Error Validation(string code, string message) =>
            new($"Validation.{code}", message);

        // Conflict (ví dụ trùng khóa, cập nhật song song)
        public static Error Conflict(string code, string message) =>
            new($"Conflict.{code}", message);

        // Not Found
        public static Error NotFound(string code, string message) =>
            new($"NotFound.{code}", message);

        // Unauthorized
        public static Error Unauthorized(string code, string message) =>
            new($"Unauthorized.{code}", message);

        // Forbidden (bị chặn truy cập)
        public static Error Forbidden(string code, string message) =>
            new($"Forbidden.{code}", message);

        // Unexpected / Unhandled
        public static Error Unexpected(string code, string message) =>
            new($"Unexpected.{code}", message);
    }
}