namespace ControlHub.Application.Accounts.DTOs
{
    public class SignInDTO
    {
        public Guid AccountId { get; set; }
        public string? Username { get; set; }
        public string accessToken { get; set; }

        public SignInDTO(Guid id, string? username, string token)
        {
            AccountId = id;
            Username = username;
            accessToken = token;
        }
    }
}
