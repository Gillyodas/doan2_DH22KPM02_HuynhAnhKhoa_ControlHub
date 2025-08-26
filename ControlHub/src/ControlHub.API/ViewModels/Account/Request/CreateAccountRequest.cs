namespace ControlHub.API.ViewModels.Account.Request
{
    public class CreateAccountRequest
    {
        public string Email { get; set; } = null!;
        public string Password { get; set; } = null!;
    }

}
