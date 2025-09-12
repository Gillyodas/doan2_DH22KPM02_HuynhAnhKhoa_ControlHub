namespace ControlHub.API.Accounts.ViewModels.Response
{
    public class SignInResponse
    {
        public Guid AccountId { get; set; }
        public string Username { get; set; }
        public string Message { get; set; }
    }
}
