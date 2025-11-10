using ControlHub.SharedKernel.Common.Logs;

namespace ControlHub.SharedKernel.Tokens
{
    public static class TokenLogs
    {
        public static readonly LogCode Refresh_Started =
            new("Token.Refresh.Started", "Starting refresh access token process");

        public static readonly LogCode Refresh_NotFound =
            new("Token.Refresh.NotFound", "Refresh token not found");

        public static readonly LogCode Refresh_AccountNotFound =
            new("Token.Refresh.AccountNotFound", "Associated account not found");

        public static readonly LogCode Refresh_TokenMismatch =
            new("Token.Refresh.TokenMismatch", "Refresh token does not belong to the account");

        public static readonly LogCode Refresh_TokenInvalid =
            new("Token.Refresh.TokenInvalid", "Refresh token is expired or already used");

        public static readonly LogCode Refresh_AccessMismatch =
            new("Token.Refresh.AccessMismatch", "Access token does not belong to the account");

        public static readonly LogCode Refresh_Valid =
            new("Token.Refresh.Valid", "Refresh token validated successfully");

        public static readonly LogCode Refresh_Success =
            new("Token.Refresh.Success", "Access and refresh tokens generated successfully");
    }
}
