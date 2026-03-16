using System.Text.Json.Serialization;

namespace ControlHub.Application.Identity.DTOs
{
    public record AccountDto(
        [property: JsonPropertyName("id")] Guid Id,
        [property: JsonPropertyName("username")] string Username,
        [property: JsonPropertyName("roleName")] string RoleName,
        [property: JsonPropertyName("isActive")] bool IsActive
    );
}
