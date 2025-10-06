namespace ControlHub.API.Accounts.ViewModels.Response
{
    public class SignInResponse
    {
        public Guid accountId { get; set; }
        public string username { get; set; } = null!;
        public string accessToken { get; set; } = null!;
        public string refreshToken { get; set; } = null!;
        public string? message { get; set; }
    }
}
