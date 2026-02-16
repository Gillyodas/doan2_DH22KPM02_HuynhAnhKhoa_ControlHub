using ControlHub.SharedKernel.Common.Errors;

namespace ControlHub.SharedKernel.Accounts
{
    public static class AccountErrors
    {
        public static readonly Error AccountNotFound =
            Error.NotFound("Account.NotFound", "The account was not found.");

        public static readonly Error AccountIdRequired =
            Error.Validation("Account.AccountIdRequired", "Account ID is required.");

        public static readonly Error EmailRequired =
            Error.Validation("Account.EmailRequired", "Email is required.");

        public static readonly Error MasterKeyRequired =
            Error.Validation("Account.MasterKeyRequired", "MasterKey is required.");

        public static readonly Error InvalidEmail =
            Error.Validation("Account.InvalidEmail", "Email format is invalid.");

        public static readonly Error EmailAlreadyExists = Error.Conflict(
        "Account.EmailAlreadyExists", "The email provided is already in use.");

        public static readonly Error EmailNotFound =
            Error.NotFound("Account.EmailNotFound", "Email does not exist.");

        public static readonly Error PasswordRequired =
            Error.Validation("Account.PasswordRequired", "Password cannot be empty.");

        public static readonly Error PasswordIsWeak =
            Error.Validation("Account.PasswordIsWeak", "Password is weak.");

        public static readonly Error PasswordMissingUppercase =
            Error.Validation("Account.PasswordMissingUppercase", "Password must contain at least one uppercase letter.");

        public static readonly Error PasswordMissingLowercase =
            Error.Validation("Account.PasswordMissingLowercase", "Password must contain at least one lowercase letter.");

        public static readonly Error PasswordMissingDigit =
            Error.Validation("Account.PasswordMissingDigit", "Password must contain at least one digit.");

        public static readonly Error PasswordMissingSpecial =
            Error.Validation("Account.PasswordMissingSpecial", "Password must contain at least one special character (!@#$%^&*()).");

        public static readonly Error PasswordHashFailed =
            Error.Failure("Account.PasswordHashFailed", "Password hashing failed.");

        public static readonly Error PasswordVerifyFailed =
            Error.Failure("Account.PasswordVerifyFailed", "Password verification failed.");

        public static readonly Error PasswordIsNotValid =
            Error.Validation("Account.PasswordIsNotValid", "Password is not valid.");

        public static readonly Error PasswordTooShort =
            Error.Validation("Account.PasswordTooShort", "Password is too short.");

        public static readonly Error PasswordSameAsOld =
            Error.Validation("Account.PasswordSameAsOld", "New password must not be the same as the current password.");

        public static readonly Error InvalidCredentials = Error.Unauthorized(
        "Auth.InvalidCredentials", "Invalid email or password.");

        public static readonly Error LockedOut =
            Error.Unauthorized("Account.LockedOut", "Account is temporarily locked due to multiple failed login attempts.");

        public static readonly Error UnsupportedIdentifierType =
            Error.Validation("Identifier.UnsupportedType", "Unsupported identifier type.");

        public static readonly Error IdentifierNotFound = Error.NotFound(
        "Account.IdentifierNotFound", "Identifier not found.");

        public static readonly Error IdentifierRequired =
            Error.Validation("Identifier.Required", "The identifier is required.");

        public static readonly Error IdentifierTooLong =
            Error.Validation("Identifier.TooLong", "The identifier is too long.");

        public static readonly Error IdentifierAlreadyExists =
            Error.Conflict("Account.IdentifierAlreadyExists", "Identifier is already registered.");

        public static readonly Error UnexpectedError =
            Error.Failure("Account.UnexpectedError", "An unexpected error occurred. Please try again later.");

        public static readonly Error AccountDisabled =
            Error.Forbidden("Account.Disabled", "Account has been disabled.");

        public static readonly Error RoleRequired =
            Error.Validation("Account.RoleRequired", "Account required role");

        public static readonly Error AccountDeleted = Error.Failure(
        "Account.Deleted", "The account has been deleted and cannot perform this action.");
    }
}
