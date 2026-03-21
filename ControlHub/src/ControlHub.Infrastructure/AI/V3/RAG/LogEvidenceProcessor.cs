using System.Text;
using System.Text.RegularExpressions;
using ControlHub.Application.AuditAI.Interfaces.V3.RAG;
using Microsoft.Extensions.Logging;

namespace ControlHub.Infrastructure.AI.V3.RAG
{
    /// <summary>
    /// Processes raw log evidence: filters noise, extracts metadata, and formats a summary.
    /// Uses structured metadata from LogEntry when available, falls back to regex on content.
    /// </summary>
    public class LogEvidenceProcessor : ILogEvidenceProcessor
    {
        private readonly ILogger<LogEvidenceProcessor> _logger;

        // Noise patterns — these INFO messages are framework internals, not actionable
        private static readonly string[] NoisePatterns =
        {
            "CORS policy execution",
            "Route matched with",
            "Executing endpoint",
            "Executed endpoint",
            "Executing action method",
            "Executed action method",
            "Executing controller action with signature",
            "PermissionClaimsTransformation",
            "PermissionAuthorizationHandler",
            "SuperAdmin detected",
            "Start processing HTTP request",
            "Sending HTTP request",
            "Received HTTP response headers",
            "End processing HTTP request",
            "Executing OkObjectResult",
            "Executing BadRequestObjectResult",
            "Validation state: Valid"
        };

        // Regex fallbacks for when structured metadata is not available
        private static readonly Regex HttpStatusRegex = new(
            @"Request finished.*?-\s*(\d{3})\s", RegexOptions.Compiled);

        private static readonly Regex ErrorCodeRegex = new(
            @"(\w+\.\w+(?:Error|Format|Exception|NotFound|Unauthorized|Forbidden|Conflict|Duplicate|Invalid\w*))",
            RegexOptions.Compiled);

        // Updated to handle both text format and Serilog compact JSON @m format
        private static readonly Regex EndpointRegex = new(
            @"(?:""?(GET|POST|PUT|DELETE|PATCH)""?\s+""?(?:https?://[^\s""]+)""*""?(/[^\s""]+)""?|"
            + @"(GET|POST|PUT|DELETE|PATCH)\s+(https?://[^\s]+))",
            RegexOptions.Compiled);

        public LogEvidenceProcessor(ILogger<LogEvidenceProcessor> logger)
        {
            _logger = logger;
        }

        public EvidenceSummary ProcessLogs(IReadOnlyList<RetrievedDocument> rawLogs)
        {
            _logger.LogInformation("Processing {Count} raw log entries for evidence extraction", rawLogs.Count);

            // Step 1: Severity Filter
            var prioritized = FilterAndPrioritize(rawLogs);

            // Step 2: Metadata Extraction (structured fields first, regex fallback)
            var metadata = ExtractMetadata(rawLogs);

            // Step 3: Format Summary
            var summary = FormatSummary(metadata, prioritized);

            _logger.LogInformation(
                "Evidence processed: {PriorityCount} priority logs, HTTP {Status}, Endpoint: {Endpoint}, Error: {ErrorCode}",
                prioritized.Count, metadata.HttpStatusCode ?? "N/A",
                metadata.AffectedEndpoint ?? "N/A", metadata.ErrorCode ?? "N/A");

            return new EvidenceSummary(prioritized, metadata, summary);
        }

        /// <summary>
        /// Filter noise and prioritize ERROR/WARNING entries.
        /// </summary>
        private List<RetrievedDocument> FilterAndPrioritize(IReadOnlyList<RetrievedDocument> rawLogs)
        {
            var result = new List<(RetrievedDocument Doc, int Priority)>();

            foreach (var log in rawLogs)
            {
                var level = log.Metadata.GetValueOrDefault("level", "Information");
                var content = log.Content;

                // Priority: 0 = highest (ERROR), 1 = WARNING, 2 = important INFO, 3 = other INFO
                int priority;
                float scoreBoost;

                if (level.Equals("Error", StringComparison.OrdinalIgnoreCase) ||
                    level.Equals("Fatal", StringComparison.OrdinalIgnoreCase) ||
                    level.Equals("Critical", StringComparison.OrdinalIgnoreCase))
                {
                    priority = 0;
                    scoreBoost = 1.0f;
                }
                else if (level.Equals("Warning", StringComparison.OrdinalIgnoreCase))
                {
                    priority = 1;
                    scoreBoost = 0.95f;
                }
                else if (IsImportantInfo(content))
                {
                    priority = 2;
                    scoreBoost = 0.8f;
                }
                else if (IsNoise(content))
                {
                    continue; // Skip noise entirely
                }
                else
                {
                    priority = 3;
                    scoreBoost = 0.5f;
                }

                var boostedDoc = new RetrievedDocument(
                    log.Content,
                    scoreBoost,
                    log.Metadata
                );
                result.Add((boostedDoc, priority));
            }

            return result
                .OrderBy(x => x.Priority)
                .Select(x => x.Doc)
                .ToList();
        }

        /// <summary>
        /// Check if an INFO log is important enough to keep.
        /// </summary>
        private static bool IsImportantInfo(string content)
        {
            return content.Contains("Request finished", StringComparison.OrdinalIgnoreCase)
                || content.Contains("Request starting", StringComparison.OrdinalIgnoreCase)
                || content.Contains("BadRequest", StringComparison.OrdinalIgnoreCase)
                || content.Contains("Exception", StringComparison.OrdinalIgnoreCase)
                || content.Contains("failed", StringComparison.OrdinalIgnoreCase)
                || content.Contains("error", StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Check if a log entry is framework noise that should be filtered.
        /// </summary>
        private static bool IsNoise(string content)
        {
            return NoisePatterns.Any(p => content.Contains(p, StringComparison.OrdinalIgnoreCase));
        }

        /// <summary>
        /// Extract structured metadata from log entries.
        /// Priority: structured metadata fields > regex fallback on content.
        /// </summary>
        private LogMetadata ExtractMetadata(IReadOnlyList<RetrievedDocument> logs)
        {
            string? httpStatus = null;
            string? errorCode = null;
            string? endpoint = null;
            string? errorMessage = null;
            string? firstTimestamp = null;
            string? lastTimestamp = null;
            int errorCount = 0, warnCount = 0, infoCount = 0;

            // First pass: extract from structured metadata (100% reliable)
            string? method = null;
            string? requestPath = null;

            foreach (var log in logs)
            {
                var level = log.Metadata.GetValueOrDefault("level", "Information");

                // Count severity distribution
                if (level.Equals("Error", StringComparison.OrdinalIgnoreCase) ||
                    level.Equals("Fatal", StringComparison.OrdinalIgnoreCase))
                    errorCount++;
                else if (level.Equals("Warning", StringComparison.OrdinalIgnoreCase))
                    warnCount++;
                else
                    infoCount++;

                // Structured: HTTP status from metadata (set by Serilog on "Request finished")
                if (httpStatus == null && log.Metadata.TryGetValue("statusCode", out var sc))
                    httpStatus = sc;

                // Structured: Method and RequestPath from metadata
                if (method == null && log.Metadata.TryGetValue("method", out var m))
                    method = m;
                if (requestPath == null && log.Metadata.TryGetValue("requestPath", out var rp))
                    requestPath = rp;

                // Structured: Timestamp from ISO metadata
                if (log.Metadata.TryGetValue("timestamp", out var ts))
                {
                    if (DateTime.TryParse(ts, out var dt))
                    {
                        var timeStr = dt.ToString("HH:mm:ss.fff");
                        firstTimestamp ??= timeStr;
                        lastTimestamp = timeStr;
                    }
                }
            }

            // Build endpoint from structured fields
            if (method != null && requestPath != null)
                endpoint = $"{method} {requestPath}";

            // Second pass: regex fallback for anything not found via metadata
            foreach (var log in logs)
            {
                var content = log.Content;
                var level = log.Metadata.GetValueOrDefault("level", "Information");

                // Fallback: HTTP status from "Request finished" content
                if (httpStatus == null)
                {
                    var statusMatch = HttpStatusRegex.Match(content);
                    if (statusMatch.Success)
                        httpStatus = statusMatch.Groups[1].Value;
                }

                // Extract domain error code (always from content — not available as metadata)
                if (errorCode == null)
                {
                    var codeMatch = ErrorCodeRegex.Match(content);
                    if (codeMatch.Success)
                        errorCode = codeMatch.Groups[1].Value;
                }

                // Fallback: endpoint from content
                if (endpoint == null)
                {
                    var endpointMatch = EndpointRegex.Match(content);
                    if (endpointMatch.Success)
                    {
                        // Match group 1+2 = Serilog compact format, group 3+4 = standard text format
                        var verb = endpointMatch.Groups[1].Success
                            ? endpointMatch.Groups[1].Value : endpointMatch.Groups[3].Value;
                        var path = endpointMatch.Groups[2].Success
                            ? endpointMatch.Groups[2].Value : endpointMatch.Groups[4].Value;
                        endpoint = $"{verb} {path}";
                    }
                }

                // Error message: use raw @m from WARNING/ERROR entries directly
                if (errorMessage == null && (level.Equals("Warning", StringComparison.OrdinalIgnoreCase)
                    || level.Equals("Error", StringComparison.OrdinalIgnoreCase)))
                {
                    // Content format: "[timestamp] [Level] message..."
                    // Extract message after the level bracket
                    var bracketEnd = content.IndexOf(']', content.IndexOf(']') + 1);
                    if (bracketEnd >= 0 && bracketEnd + 1 < content.Length)
                    {
                        errorMessage = content[(bracketEnd + 1)..].Trim();
                    }
                    else
                    {
                        // Direct @m content (no brackets)
                        errorMessage = content.Trim();
                    }

                    // Truncate very long messages for the summary
                    if (errorMessage.Length > 300)
                        errorMessage = errorMessage[..300] + "...";
                }
            }

            var timestampRange = (firstTimestamp != null && lastTimestamp != null)
                ? $"{firstTimestamp} – {lastTimestamp}"
                : null;

            return new LogMetadata(
                HttpStatusCode: httpStatus,
                ErrorCode: errorCode,
                AffectedEndpoint: endpoint,
                ErrorMessage: errorMessage,
                TimestampRange: timestampRange,
                ErrorCount: errorCount,
                WarningCount: warnCount,
                InfoCount: infoCount
            );
        }

        /// <summary>
        /// Format a human-readable evidence summary for LLM prompt injection.
        /// </summary>
        private string FormatSummary(LogMetadata metadata, List<RetrievedDocument> prioritized)
        {
            var sb = new StringBuilder();
            sb.AppendLine("## Evidence Summary (auto-extracted, use as ground truth)");

            if (metadata.AffectedEndpoint != null || metadata.HttpStatusCode != null)
                sb.AppendLine($"- **Request**: {metadata.AffectedEndpoint ?? "Unknown"} → HTTP {metadata.HttpStatusCode ?? "N/A"}");

            if (metadata.ErrorCode != null)
                sb.AppendLine($"- **Error Code**: {metadata.ErrorCode}");

            if (metadata.ErrorMessage != null)
                sb.AppendLine($"- **Error Message**: {metadata.ErrorMessage}");

            if (metadata.TimestampRange != null)
                sb.AppendLine($"- **Timeline**: {metadata.TimestampRange}");

            sb.AppendLine($"- **Severity**: {metadata.ErrorCount} ERROR, {metadata.WarningCount} WARNING, {metadata.InfoCount} INFO");
            sb.AppendLine();

            // Add key WARNING/ERROR entries
            var keyEntries = prioritized
                .Where(p => p.Metadata.GetValueOrDefault("level", "") is "Error" or "Warning" or "Fatal" or "Critical")
                .Take(5)
                .ToList();

            if (keyEntries.Any())
            {
                sb.AppendLine("## Key Log Entries (WARNING/ERROR):");
                foreach (var entry in keyEntries)
                {
                    sb.AppendLine($"  {entry.Content}");
                }
            }

            return sb.ToString();
        }
    }
}
