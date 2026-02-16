namespace ControlHub.API.Accounts.ViewModels.Request
{
    public class SignOutRequest
    {
        public string accessToken { get; set; } = null!;
        public string refreshToken { get; set; } = null!;
    }
}
