namespace ControlHub.Application.Roles.DTOs
{
    public record RolePermissionDetailDto(
        Guid RoleId,
        string RoleName,
        Guid PermissionId,
        string PermissionName
    );
}
