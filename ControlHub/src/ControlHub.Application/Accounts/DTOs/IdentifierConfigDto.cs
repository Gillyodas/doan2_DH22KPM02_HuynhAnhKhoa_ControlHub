namespace ControlHub.Application.Accounts.DTOs
{
    public record IdentifierConfigDto(
        Guid Id,
        string Name,
        string Description,
        bool IsActive,
        List<ValidationRuleDto> Rules
    );
}
