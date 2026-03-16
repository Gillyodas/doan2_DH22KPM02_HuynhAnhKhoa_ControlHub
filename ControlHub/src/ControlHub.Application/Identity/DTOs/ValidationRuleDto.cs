using System.Text.Json.Serialization;
using ControlHub.Domain.Identity.Enums;

namespace ControlHub.Application.Identity.DTOs
{
    public record ValidationRuleDto(
        [property: JsonPropertyName("type")] ValidationRuleType Type,
        [property: JsonPropertyName("parameters")] Dictionary<string, object> Parameters,
        [property: JsonPropertyName("errorMessage")] string? ErrorMessage = null,
        [property: JsonPropertyName("order")] int Order = 0
    );
}
