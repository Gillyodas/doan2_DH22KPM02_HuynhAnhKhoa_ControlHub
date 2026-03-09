namespace ControlHub.Application.AccessControl.DTOs
{
    public record RoleDto(
        Guid Id,
        string Name,
        string Description
    );
}
