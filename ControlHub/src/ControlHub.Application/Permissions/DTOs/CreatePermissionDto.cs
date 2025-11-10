namespace ControlHub.Application.Permissions.DTOs
{
    public sealed record CreatePermissionDto(
        string Code,
        string? Description
        );
}
