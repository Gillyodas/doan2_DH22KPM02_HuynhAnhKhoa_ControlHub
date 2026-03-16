using System.Text.Json.Serialization;

namespace ControlHub.API.Identity.ViewModels.Response
{
    public class UpdateUsernameResponse
    {
        [JsonPropertyName("username")]
        public string? Username { get; set; }

        [JsonPropertyName("message")]
        public string? Message { get; set; }
    }
}
