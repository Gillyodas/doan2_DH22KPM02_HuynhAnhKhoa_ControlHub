namespace ControlHub.API.Accounts.ViewModels.Request
{
    public class RegisterRequest
    {
        public string Value { get; set; } = null!;
        public string Password { get; set; } = null!;
        public string Type { get; set; } = null!;
    }

}