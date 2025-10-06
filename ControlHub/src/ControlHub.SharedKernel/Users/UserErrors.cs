using ControlHub.SharedKernel.Common.Errors;

namespace ControlHub.SharedKernel.Users
{
    public static class UserErrors
    {
        public static readonly Error NotFound =
        new("User.NotFound", "User not found.");

        public static readonly Error Unauthorized =
            new("User.Unauthorized", "User is not authorized to perform this action.");

        public static readonly Error Inactive =
            new("User.Inactive", "User account is inactive.");

        public static readonly Error DuplicateUsername =
            new("User.DuplicateUsername", "Username is already taken.");

        public static readonly Error ProfileUpdateFailed =
            new("User.ProfileUpdateFailed", "Failed to update user profile.");

        public static readonly Error UnexpectedError =
            new("User.UnexpectedError", "An unexpected error occurred. Please try again later.");

        public static readonly Error Required =
            new("User.Required", "User is required.");

        public static readonly Error AlreadyAtached =
            new("User.AlreadyAtached", "User is already atached.");
    }
}
