using ControlHub.SharedKernel.Common.Errors;

namespace ControlHub.SharedKernel.Tokens
{
    public class TokenErrors
    {
        public static readonly Error TokenAlreadyUsed =
            new("Token.AlreadyUsed", "Token is already used.");

        public static readonly Error TokenExpired =
            new("Token.Expired", "Token is expired.");

        public static readonly Error TokenRequired =
            new("Token.Required", "Token is not empty.");

        public static readonly Error TokenNotFound =
            new("Token.NotFound", "Token is not found.");

        public static readonly Error TokenInvalid =
            new("Token.Invalid", "Token is invalid.");

        public static readonly Error TokenNotBelongToAccount =
            new("Token.NotBelongToAccount", "Token does not belong to this account.");

        public static readonly Error TokenGenerationFailed = new(
            "Token.GenerationFailed", "Failed to generate access or refresh token.");
    }
}
