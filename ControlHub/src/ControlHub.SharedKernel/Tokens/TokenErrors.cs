using ControlHub.SharedKernel.Common.Errors;

namespace ControlHub.SharedKernel.Tokens
{
    public class TokenErrors
    {
        public static readonly Error TokenAlreadyUsed =
            Error.Conflict("Token.AlreadyUsed", "Token is already used.");

        public static readonly Error TokenExpired =
            Error.Unauthorized("Token.Expired", "Token is expired.");

        public static readonly Error TokenRequired =
            Error.Validation("Token.Required", "Token is not empty.");

        public static readonly Error TokenNotFound =
            Error.NotFound("Token.NotFound", "Token is not found.");

        public static readonly Error TokenInvalid =
            Error.Unauthorized("Token.Invalid", "Token is invalid.");

        public static readonly Error TokenNotBelongToAccount =
            Error.Forbidden("Token.NotBelongToAccount", "Token does not belong to this account.");

        public static readonly Error TokenGenerationFailed =
            Error.Failure("Token.GenerationFailed", "Failed to generate access or refresh token.");

        public static readonly Error TokenAlreadyRevoked =
            Error.Conflict("Token.AlreadyRevoked", "Token is already revoked.");
    }
}
