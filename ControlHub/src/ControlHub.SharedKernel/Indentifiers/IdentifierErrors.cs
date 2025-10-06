using ControlHub.SharedKernel.Common.Errors;

namespace ControlHub.SharedKernel.Indentifiers
{
    public class IdentifierErrors
    {
        public static readonly Error UsernameRequired =
            new("Identifier.UsernameRequired", "Username cannot be empty");

        public static readonly Error UsernameLengthInvalid =
            new("Identifier.UsernameLengthInvalid", "Username must be between 3 and 30 characters");

        public static readonly Error UsernameInvalidCharacters =
            new("Identifier.UsernameInvalidCharacters", "Username contains invalid characters");

        public static readonly Error PhoneRequired =
            new("Identifier.PhoneRequired", "Phone number cannot be empty");

        public static readonly Error PhoneInvalidFormat =
            new("Identifier.PhoneInvalidFormat", "Invalid phone number format");
    }
}
