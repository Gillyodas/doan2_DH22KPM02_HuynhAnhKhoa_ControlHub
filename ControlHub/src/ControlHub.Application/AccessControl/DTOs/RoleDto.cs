using System.Text.Json.Serialization;

namespace ControlHub.Application.AccessControl.DTOs
{
    public record RoleDto(
        [property: JsonPropertyName("id")] Guid Id,
        [property: JsonPropertyName("name")] string Name,
        [property: JsonPropertyName("description")] string Description
    );
}
