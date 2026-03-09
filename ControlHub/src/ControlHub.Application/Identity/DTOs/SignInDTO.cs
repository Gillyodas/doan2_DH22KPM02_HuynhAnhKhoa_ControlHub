namespace ControlHub.Application.Identity.DTOs
{
    public record SignInDTO(Guid AccountId, string Username, string AccessToken, string RefreshToken);
}
