namespace ControlHub.Application.AccessControl.DTOs
{
    public record RolePermissionDetailDto(
        Guid RoleId,
        string RoleName,
        Guid PermissionId,
        string PermissionName
    );
}
