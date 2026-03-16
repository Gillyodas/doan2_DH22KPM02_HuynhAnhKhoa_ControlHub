using System.Text.Json.Serialization;

namespace ControlHub.Application.Identity.DTOs
{
    public record UserDto(
        [property: JsonPropertyName("id")] Guid Id,
        [property: JsonPropertyName("username")] string Username,
        [property: JsonPropertyName("email")] string? Email,
        [property: JsonPropertyName("firstName")] string? FirstName,
        [property: JsonPropertyName("lastName")] string? LastName,
        [property: JsonPropertyName("phoneNumber")] string? PhoneNumber,
        [property: JsonPropertyName("isActive")] bool IsActive,
        [property: JsonPropertyName("roleId")] Guid RoleId,
        [property: JsonPropertyName("roleName")] string? RoleName
    );
}
