using System.Text.Json.Serialization;

namespace ControlHub.API.Identity.ViewModels.Response
{
    public class SignInResponse
    {
        [JsonPropertyName("accountId")]
        public Guid AccountId { get; set; }

        [JsonPropertyName("username")]
        public string Username { get; set; } = null!;

        [JsonPropertyName("accessToken")]
        public string AccessToken { get; set; } = null!;

        [JsonPropertyName("refreshToken")]
        public string RefreshToken { get; set; } = null!;

        [JsonPropertyName("message")]
        public string? Message { get; set; }
    }
}
