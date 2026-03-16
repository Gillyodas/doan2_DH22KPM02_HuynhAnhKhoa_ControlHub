using System.Text.Json.Serialization;

namespace ControlHub.API.Identity.ViewModels.Response
{
    public class RefreshAccessTokenResponse
    {
        [JsonPropertyName("refreshToken")]
        public string RefreshToken { get; set; } = null!;

        [JsonPropertyName("accessToken")]
        public string AccessToken { get; set; } = null!;

        [JsonPropertyName("message")]
        public string? Message { get; set; }
    }
}
