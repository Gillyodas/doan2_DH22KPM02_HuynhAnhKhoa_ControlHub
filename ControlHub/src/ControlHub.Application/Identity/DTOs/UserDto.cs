namespace ControlHub.Application.Identity.DTOs
{
    public record UserDto(
        Guid Id,
        string Username,
        string? Email,
        string? FirstName,
        string? LastName,
        string? PhoneNumber,
        bool IsActive,
        Guid RoleId,
        string? RoleName
    );
}
