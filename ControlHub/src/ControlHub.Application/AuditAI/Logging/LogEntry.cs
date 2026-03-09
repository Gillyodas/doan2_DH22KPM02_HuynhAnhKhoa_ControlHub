namespace ControlHub.Application.AuditAI.Logging
{
    public class LogEntry
    {
        public DateTime Timestamp { get; set; }
        public string Level { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public string? RenderedMessage { get; set; }
        public string? SourceContext { get; set; }
        public string? CorrelationId { get; set; }
        public string? RequestId { get; set; }
        public string? TraceId { get; set; }
        public string? SerilogTraceId { get; set; }
        public string? Exception { get; set; }
        public Dictionary<string, object> Properties { get; set; } = new();
    }
}
