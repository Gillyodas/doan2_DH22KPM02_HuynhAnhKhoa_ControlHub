namespace ControlHub.Application.Accounts.Commands.RefreshAccessToken
{
    public sealed record RefreshAccessTokenResponse(string AccessToken, string RefreshToken);
}
