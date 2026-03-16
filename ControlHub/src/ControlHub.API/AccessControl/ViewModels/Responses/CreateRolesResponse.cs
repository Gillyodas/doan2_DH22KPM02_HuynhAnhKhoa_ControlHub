using System.Text.Json.Serialization;

namespace ControlHub.API.AccessControl.ViewModels.Responses
{
    public class CreateRolesResponse
    {
        [JsonPropertyName("message")]
        public string Message { get; set; } = string.Empty;

        [JsonPropertyName("successCount")]
        public int SuccessCount { get; set; }

        [JsonPropertyName("failureCount")]
        public int FailureCount { get; set; }

        [JsonPropertyName("failedRoles")]
        public IEnumerable<string>? FailedRoles { get; set; }
    }
}
