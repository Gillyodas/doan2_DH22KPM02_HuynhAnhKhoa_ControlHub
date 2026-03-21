using System.Text.Json;
using System.Text.Json.Serialization;

namespace ControlHub.Application.AuditAI.Logging
{
    /// <summary>
    /// Custom converter that maps Serilog Compact JSON format (@t, @m, @l, @tr, ...)
    /// AND regular top-level properties (RequestId, SourceContext, ...) into LogEntry.
    /// </summary>
    public class LogEntryJsonConverter : JsonConverter<LogEntry>
    {
        // Known Serilog compact fields (@ prefix)
        // @t  = Timestamp
        // @m  = RenderedMessage (message with values filled in)
        // @mt = MessageTemplate (raw template - e.g. "User {UserId} logged in")
        // @l  = Level (omitted when Information)
        // @x  = Exception
        // @tr = TraceId (OpenTelemetry)
        // @sp = SpanId
        // @i  = EventId hash
        // @r  = Renderings

        // Known top-level properties (written by ASP.NET Core / Serilog enrichers)
        private static readonly HashSet<string> KnownProperties = new(StringComparer.OrdinalIgnoreCase)
        {
            "@t", "@m", "@mt", "@l", "@x", "@tr", "@sp", "@i", "@r",
            "SourceContext", "RequestId", "RequestPath", "ConnectionId",
            "TraceId", "SpanId", "Application", "CorrelationId"
        };

        public override LogEntry Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (reader.TokenType != JsonTokenType.StartObject)
                throw new JsonException("Expected StartObject");

            var entry = new LogEntry();

            while (reader.Read())
            {
                if (reader.TokenType == JsonTokenType.EndObject)
                    break;

                if (reader.TokenType != JsonTokenType.PropertyName)
                    throw new JsonException("Expected PropertyName");

                var propertyName = reader.GetString()!;
                reader.Read(); // Move to value

                switch (propertyName)
                {
                    // ── Serilog compact fields ──
                    case "@t":
                        if (reader.TokenType == JsonTokenType.String &&
                            DateTime.TryParse(reader.GetString(), out var ts))
                            entry.Timestamp = ts;
                        else
                            reader.TrySkip();
                        break;

                    case "@m":
                        entry.Message = reader.GetString() ?? string.Empty;
                        break;

                    case "@mt":
                        // MessageTemplate — store as RenderedMessage fallback
                        entry.RenderedMessage ??= reader.GetString();
                        break;

                    case "@l":
                        entry.Level = reader.GetString() ?? "Information";
                        break;

                    case "@x":
                        entry.Exception = reader.GetString();
                        break;

                    case "@tr":
                        entry.SerilogTraceId = reader.GetString();
                        break;

                    case "@sp":
                        entry.SpanId = reader.GetString();
                        break;

                    case "@i":
                        // EventId hash — store in Properties
                        entry.Properties["EventIdHash"] = reader.GetString() ?? "";
                        break;

                    case "@r":
                        // Renderings array — skip for now
                        reader.TrySkip();
                        break;

                    // ── Standard top-level properties ──
                    case "SourceContext":
                        entry.SourceContext = reader.GetString();
                        break;

                    case "RequestId":
                        entry.RequestId = reader.GetString();
                        break;

                    case "RequestPath":
                        entry.Properties["RequestPath"] = reader.GetString() ?? "";
                        break;

                    case "ConnectionId":
                        entry.Properties["ConnectionId"] = reader.GetString() ?? "";
                        break;

                    case "TraceId":
                        entry.TraceId = reader.GetString();
                        break;

                    case "CorrelationId":
                        entry.CorrelationId = reader.GetString();
                        break;

                    case "Application":
                        entry.Properties["Application"] = reader.GetString() ?? "";
                        break;

                    // ── Everything else → Properties ──
                    default:
                        entry.Properties[propertyName] = ReadValue(ref reader);
                        break;
                }
            }

            // Serilog compact format omits @l for Information level
            if (string.IsNullOrEmpty(entry.Level))
                entry.Level = "Information";

            // If @m was empty but @mt exists, use template as message
            if (string.IsNullOrEmpty(entry.Message) && !string.IsNullOrEmpty(entry.RenderedMessage))
                entry.Message = entry.RenderedMessage;

            return entry;
        }

        public override void Write(Utf8JsonWriter writer, LogEntry value, JsonSerializerOptions options)
        {
            writer.WriteStartObject();

            writer.WriteString("@t", value.Timestamp.ToString("O"));
            writer.WriteString("@m", value.Message);

            if (value.Level != "Information")
                writer.WriteString("@l", value.Level);

            if (value.RenderedMessage != null)
                writer.WriteString("@mt", value.RenderedMessage);

            if (value.Exception != null)
                writer.WriteString("@x", value.Exception);

            if (value.SerilogTraceId != null)
                writer.WriteString("@tr", value.SerilogTraceId);

            if (value.SpanId != null)
                writer.WriteString("@sp", value.SpanId);

            if (value.SourceContext != null)
                writer.WriteString("SourceContext", value.SourceContext);

            if (value.RequestId != null)
                writer.WriteString("RequestId", value.RequestId);

            if (value.TraceId != null)
                writer.WriteString("TraceId", value.TraceId);

            if (value.CorrelationId != null)
                writer.WriteString("CorrelationId", value.CorrelationId);

            foreach (var kvp in value.Properties)
            {
                writer.WritePropertyName(kvp.Key);
                JsonSerializer.Serialize(writer, kvp.Value, options);
            }

            writer.WriteEndObject();
        }

        /// <summary>
        /// Reads any JSON value into an object (string, number, bool, or nested as raw string).
        /// </summary>
        private static object ReadValue(ref Utf8JsonReader reader)
        {
            switch (reader.TokenType)
            {
                case JsonTokenType.String:
                    return reader.GetString() ?? "";

                case JsonTokenType.Number:
                    if (reader.TryGetInt64(out var l)) return l;
                    return reader.GetDouble();

                case JsonTokenType.True:
                    return true;

                case JsonTokenType.False:
                    return false;

                case JsonTokenType.Null:
                    return "";

                case JsonTokenType.StartObject:
                case JsonTokenType.StartArray:
                    // For complex values, capture as raw JSON string
                    using (var doc = JsonDocument.ParseValue(ref reader))
                    {
                        return doc.RootElement.Clone().ToString();
                    }

                default:
                    reader.TrySkip();
                    return "";
            }
        }
    }
}
