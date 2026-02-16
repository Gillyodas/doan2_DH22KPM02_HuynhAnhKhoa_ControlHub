namespace ControlHub.API.Accounts.ViewModels.Request
{
    public class ChangePasswordRequest
    {
        public string curPass { get; set; } = null!;
        public string newPass { get; set; } = null!;
    }
}
