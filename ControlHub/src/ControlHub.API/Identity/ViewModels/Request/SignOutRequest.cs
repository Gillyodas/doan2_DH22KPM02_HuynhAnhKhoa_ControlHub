namespace ControlHub.API.Identity.ViewModels.Request
{
    public class SignOutRequest
    {
        public string AccessToken { get; set; } = null!;
        public string RefreshToken { get; set; } = null!;
    }
}
