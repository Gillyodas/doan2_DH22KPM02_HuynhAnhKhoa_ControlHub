namespace ControlHub.API.Accounts.ViewModels.Request
{
    public class ResetPasswordRequest
    {
        public string Password { get; set; } = null!;
        public string Token { get; set; } = null!;
    }
}
