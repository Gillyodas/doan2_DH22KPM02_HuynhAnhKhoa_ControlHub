using ControlHub.SharedKernel.Common.Errors;

namespace ControlHub.SharedKernel.Indentifiers
{
    public class IdentifierErrors
    {
        public static readonly Error UsernameRequired =
            Error.Validation("Identifier.UsernameRequired", "Username cannot be empty");

        public static readonly Error UsernameLengthInvalid =
            Error.Validation("Identifier.UsernameLengthInvalid", "Username must be between 3 and 30 characters");

        public static readonly Error UsernameInvalidCharacters =
            Error.Validation("Identifier.UsernameInvalidCharacters", "Username contains invalid characters");

        public static readonly Error PhoneRequired =
            Error.Validation("Identifier.PhoneRequired", "Phone number cannot be empty");

        public static readonly Error PhoneInvalidFormat =
            Error.Validation("Identifier.PhoneInvalidFormat", "Invalid phone number format");
    }
}
