using System.Text.Json.Serialization;

namespace ControlHub.API.Identity.ViewModels.Response
{
    public class RegisterAdminResponse
    {
        [JsonPropertyName("accountId")]
        public Guid AccountId { get; set; }

        [JsonPropertyName("message")]
        public string? Message { get; set; }
    }
}
