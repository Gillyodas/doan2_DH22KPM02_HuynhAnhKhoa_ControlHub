namespace ControlHub.Application.AccessControl.DTOs
{
    public sealed record CreatePermissionDto(
        string Code,
        string? Description
        );
}
