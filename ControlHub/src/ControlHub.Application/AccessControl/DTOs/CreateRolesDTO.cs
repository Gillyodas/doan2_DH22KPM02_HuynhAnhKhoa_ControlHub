namespace ControlHub.Application.AccessControl.DTOs
{
    public record CreateRoleDto(
        string Name,
        string? Description,
        IEnumerable<Guid>? PermissionIds
    );
}
