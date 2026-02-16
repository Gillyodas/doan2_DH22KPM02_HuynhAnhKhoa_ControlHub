using ControlHub.Domain.Identity.Enums;

namespace ControlHub.Application.Accounts.DTOs
{
    public record ValidationRuleDto(
    ValidationRuleType Type,
    Dictionary<string, object> Parameters,
    string? ErrorMessage = null,
    int Order = 0
);
}
