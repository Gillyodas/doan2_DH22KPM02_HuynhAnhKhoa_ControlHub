namespace ControlHub.API.Accounts.ViewModels.Request
{
    public class RefreshAccessTokenRequest
    {
        public string RefreshToken { get; set; } = null!;
        public string AccessToken { get; set; } = null!;
        public Guid AccID { get; set; }
    }
}
