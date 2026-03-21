using System.Text.Json.Serialization;

namespace ControlHub.Application.AuditAI.Logging
{
    [JsonConverter(typeof(LogEntryJsonConverter))]
    public class LogEntry
    {
        /// <summary>@t — Timestamp</summary>
        public DateTime Timestamp { get; set; }

        /// <summary>@l — Level (omitted = Information in Serilog compact format)</summary>
        public string Level { get; set; } = string.Empty;

        /// <summary>@m — Rendered message (values filled in)</summary>
        public string Message { get; set; } = string.Empty;

        /// <summary>@mt — Message template (raw template)</summary>
        public string? RenderedMessage { get; set; }

        /// <summary>SourceContext — Logger category name</summary>
        public string? SourceContext { get; set; }

        /// <summary>CorrelationId — Custom correlation ID (if set by application)</summary>
        public string? CorrelationId { get; set; }

        /// <summary>RequestId — ASP.NET Core HttpContext.TraceIdentifier (format: "0HNK72OGUNVLL:0000002B")</summary>
        public string? RequestId { get; set; }

        /// <summary>TraceId — Standard property name (non-Serilog loggers)</summary>
        public string? TraceId { get; set; }

        /// <summary>@tr — OpenTelemetry TraceId from Serilog compact format</summary>
        public string? SerilogTraceId { get; set; }

        /// <summary>@sp — OpenTelemetry SpanId from Serilog compact format</summary>
        public string? SpanId { get; set; }

        /// <summary>@x — Exception details</summary>
        public string? Exception { get; set; }

        /// <summary>All other properties not explicitly mapped</summary>
        public Dictionary<string, object> Properties { get; set; } = new();
    }
}
