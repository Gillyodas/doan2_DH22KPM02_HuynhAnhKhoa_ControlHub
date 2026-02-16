namespace ControlHub.Application.Roles.DTOs
{
    public record CreateRoleDto(
        string Name,
        string? Description,
        IEnumerable<Guid>? PermissionIds
    );
}
