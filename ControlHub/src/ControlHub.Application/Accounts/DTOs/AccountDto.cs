namespace ControlHub.Application.Accounts.DTOs
{
    public record AccountDto(Guid Id, string Username, string RoleName, bool IsActive);
}
