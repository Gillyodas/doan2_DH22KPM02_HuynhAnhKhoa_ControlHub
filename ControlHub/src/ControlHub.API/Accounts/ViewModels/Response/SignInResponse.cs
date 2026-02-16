namespace ControlHub.API.Accounts.ViewModels.Response
{
    public class SignInResponse
    {
        public Guid AccountId { get; set; }
        public string Username { get; set; } = null!;
        public string AccessToken { get; set; } = null!;
        public string RefreshToken { get; set; } = null!;
        public string? Message { get; set; }
    }
}
