using System.Text.Json.Serialization;

namespace ControlHub.Application.AccessControl.DTOs
{
    public record RolePermissionDetailDto(
        [property: JsonPropertyName("roleId")] Guid RoleId,
        [property: JsonPropertyName("roleName")] string RoleName,
        [property: JsonPropertyName("permissionId")] Guid PermissionId,
        [property: JsonPropertyName("permissionName")] string PermissionName
    );
}
