namespace ControlHub.Application.Identity.Commands.RefreshAccessToken
{
    public sealed record RefreshAccessTokenResponse(string AccessToken, string RefreshToken);
}
