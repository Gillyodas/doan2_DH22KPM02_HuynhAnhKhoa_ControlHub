using System.Text.Json.Serialization;

namespace ControlHub.Application.Identity.DTOs
{
    public record IdentifierConfigDto(
        [property: JsonPropertyName("id")] Guid Id,
        [property: JsonPropertyName("name")] string Name,
        [property: JsonPropertyName("description")] string Description,
        [property: JsonPropertyName("isActive")] bool IsActive,
        [property: JsonPropertyName("rules")] List<ValidationRuleDto> Rules
    );
}
