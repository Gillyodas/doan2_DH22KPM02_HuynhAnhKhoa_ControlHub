using System.Text.Json.Serialization;

namespace ControlHub.Application.AccessControl.DTOs
{
    public record PermissionDto(
        [property: JsonPropertyName("id")] Guid Id,
        [property: JsonPropertyName("code")] string Code,
        [property: JsonPropertyName("description")] string Description
    );
}
