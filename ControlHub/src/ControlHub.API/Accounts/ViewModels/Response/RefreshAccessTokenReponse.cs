namespace ControlHub.API.Accounts.ViewModels.Response
{
    public class RefreshAccessTokenReponse
    {
        public string RefreshToken { get; set; } = null!;
        public string AccessToken { get; set; } = null!;
        public string? Message { get; set; }
    }
}
