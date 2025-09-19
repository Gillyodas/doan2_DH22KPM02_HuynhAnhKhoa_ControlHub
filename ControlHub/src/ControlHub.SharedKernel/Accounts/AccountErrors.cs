using ControlHub.SharedKernel.Common.Errors;

namespace ControlHub.SharedKernel.Accounts
{
    public static class AccountErrors
    {
        public static readonly Error AccountNotFound =
            new("Account.NotFound", "The account was not found.");

        public static readonly Error EmailRequired =
            new("Account.EmailRequired", "Email is required.");

        public static readonly Error InvalidEmail =
            new("Account.InvalidEmail", "Email format is invalid.");

        public static readonly Error EmailAlreadyExists =
            new("Account.EmailAlreadyExists", "Email is already registered.");

        public static readonly Error EmailNotFound =
            new("Account.EmailNotFound", "Email does not exist.");

        public static readonly Error PasswordRequired =
            new("Account.PasswordRequired", "Password cannot be empty.");

        public static readonly Error PasswordTooShort =
            new("Account.PasswordTooShort", "Password must be at least 8 characters long.");

        public static readonly Error PasswordMissingUppercase =
            new("Account.PasswordMissingUppercase", "Password must contain at least one uppercase letter.");

        public static readonly Error PasswordMissingLowercase =
            new("Account.PasswordMissingLowercase", "Password must contain at least one lowercase letter.");

        public static readonly Error PasswordMissingDigit =
            new("Account.PasswordMissingDigit", "Password must contain at least one digit.");

        public static readonly Error PasswordMissingSpecial =
            new("Account.PasswordMissingSpecial", "Password must contain at least one special character (!@#$%^&*()).");

        public static readonly Error PasswordHashFailed =
            new("Account.PasswordHashFailed", "Password hashing failed.");

        public static readonly Error PasswordVerifyFailed =
            new("Account.PasswordVerifyFailed", "Password verification failed.");

        public static readonly Error PasswordIsNotValid =
            new("Account.PasswordIsNotValid", "Password is not valid.");

        public static readonly Error PasswordSameAsOld = 
            new("Account.PasswordSameAsOld", "New password must not be the same as the current password.");

        public static readonly Error InvalidCredentials =
            new("Account.InvalidCredentials", "Email or password is incorrect.");

        public static readonly Error LockedOut =
            new("Account.LockedOut", "Account is temporarily locked due to multiple failed login attempts.");

        public static readonly Error UnexpectedError =
            new("Account.UnexpectedError", "An unexpected error occurred. Please try again later.");
    }
}