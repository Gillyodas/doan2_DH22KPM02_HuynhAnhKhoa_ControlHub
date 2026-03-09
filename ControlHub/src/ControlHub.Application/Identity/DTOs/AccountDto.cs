namespace ControlHub.Application.Identity.DTOs
{
    public record AccountDto(Guid Id, string Username, string RoleName, bool IsActive);
}
