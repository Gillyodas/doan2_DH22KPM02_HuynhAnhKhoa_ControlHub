namespace ControlHub.API.Accounts.ViewModels.Request
{
    public class SignInRequest
    {
        public string Email { get; set; } = null!;
        public string Password { get; set; } = null!;
    }
}
