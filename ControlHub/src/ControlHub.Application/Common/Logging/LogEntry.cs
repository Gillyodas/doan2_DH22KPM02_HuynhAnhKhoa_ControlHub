using System.Text.Json.Serialization;

namespace ControlHub.Application.Common.Logging
{
    public class LogEntry
    {
        [JsonPropertyName("@t")]
        public DateTime Timestamp { get; set; }

        [JsonPropertyName("@mt")]
        public string MessageTemplate { get; set; } = string.Empty;

        [JsonPropertyName("@m")]
        public string? RenderedMessage { get; set; }

        [JsonIgnore]
        public string Message => !string.IsNullOrEmpty(RenderedMessage) ? RenderedMessage : MessageTemplate;

        [JsonPropertyName("@l")]
        public string Level { get; set; } = "Information";

        [JsonPropertyName("LogCode")]
        public LogCodeDto? LogCode { get; set; }

        [JsonPropertyName("RequestId")]
        public string? RequestId { get; set; }

        [JsonPropertyName("TraceId")]
        public string? TraceId { get; set; }

        [JsonPropertyName("@tr")]
        public string? SerilogTraceId { get; set; }

        [JsonExtensionData]
        public Dictionary<string, object> Properties { get; set; } = new();
    }

    public class LogCodeDto
    {
        public string Code { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
    }
}
