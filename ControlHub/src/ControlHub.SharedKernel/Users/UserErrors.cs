using ControlHub.SharedKernel.Common.Errors;

namespace ControlHub.SharedKernel.Users
{
    public static class UserErrors
    {
        public static readonly Error NotFound =
        Error.NotFound("User.NotFound", "User not found.");

        public static readonly Error Unauthorized =
            Error.Unauthorized("User.Unauthorized", "User is not authorized to perform this action.");

        public static readonly Error Inactive =
            Error.Forbidden("User.Inactive", "User account is inactive.");

        public static readonly Error DuplicateUsername =
            Error.Conflict("User.DuplicateUsername", "Username is already taken.");

        public static readonly Error ProfileUpdateFailed =
            Error.Failure("User.ProfileUpdateFailed", "Failed to update user profile.");

        public static readonly Error UnexpectedError =
            Error.Failure("User.UnexpectedError", "An unexpected error occurred. Please try again later.");

        public static readonly Error Required =
            Error.Validation("User.Required", "User is required.");

        public static readonly Error AlreadyAtached =
            Error.Conflict("User.AlreadyAtached", "User is already atached.");
    }
}
