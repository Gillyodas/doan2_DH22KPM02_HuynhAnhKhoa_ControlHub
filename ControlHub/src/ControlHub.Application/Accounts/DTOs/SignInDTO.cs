namespace ControlHub.Application.Accounts.DTOs
{
    public record SignInDTO(Guid AccountId, string Username, string AccessToken, string RefreshToken);
}
