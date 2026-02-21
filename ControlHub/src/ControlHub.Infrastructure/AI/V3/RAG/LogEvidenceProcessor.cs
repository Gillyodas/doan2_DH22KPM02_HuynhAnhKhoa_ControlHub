using System.Text;
using System.Text.RegularExpressions;
using ControlHub.Application.Common.Interfaces.AI.V3.RAG;
using Microsoft.Extensions.Logging;

namespace ControlHub.Infrastructure.AI.V3.RAG
{
    /// <summary>
    /// Processes raw log evidence: filters noise, extracts metadata, and formats a summary.
    /// All processing is code-based (regex) — no AI needed.
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

        // Regex patterns for metadata extraction
        private static readonly Regex HttpStatusRegex = new(
            @"Request finished.*?-\s*(\d{3})\s", RegexOptions.Compiled);

        private static readonly Regex ErrorCodeRegex = new(
            @"(\w+\.\w+(?:Error|Format|Exception|NotFound|Unauthorized|Forbidden|Conflict|Duplicate|Invalid\w*))",
            RegexOptions.Compiled);

        private static readonly Regex EndpointRegex = new(
            @"(GET|POST|PUT|DELETE|PATCH)\s+(https?://[^\s]+)", RegexOptions.Compiled);

        private static readonly Regex TimestampRegex = new(
            @"\[(\d{2}:\d{2}:\d{2})", RegexOptions.Compiled);

        public LogEvidenceProcessor(ILogger<LogEvidenceProcessor> logger)
        {
            _logger = logger;
        }

        public EvidenceSummary ProcessLogs(IReadOnlyList<RetrievedDocument> rawLogs)
        {
            _logger.LogInformation("Processing {Count} raw log entries for evidence extraction", rawLogs.Count);

            // Step 1: Severity Filter
            var prioritized = FilterAndPrioritize(rawLogs);

            // Step 2: Metadata Extraction
            var metadata = ExtractMetadata(rawLogs);

            // Step 3: Format Summary
            var summary = FormatSummary(metadata, prioritized);

            _logger.LogInformation(
                "Evidence processed: {PriorityCount} priority logs, HTTP {Status}, Error: {ErrorCode}",
                prioritized.Count, metadata.HttpStatusCode ?? "N/A", metadata.ErrorCode ?? "N/A");

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
        /// Extract structured metadata from log entries using regex (100% accurate, no AI).
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

            foreach (var log in logs)
            {
                var content = log.Content;
                var level = log.Metadata.GetValueOrDefault("level", "Information");

                // Count severity distribution
                if (level.Equals("Error", StringComparison.OrdinalIgnoreCase) ||
                    level.Equals("Fatal", StringComparison.OrdinalIgnoreCase))
                    errorCount++;
                else if (level.Equals("Warning", StringComparison.OrdinalIgnoreCase))
                    warnCount++;
                else
                    infoCount++;

                // Extract HTTP status from "Request finished" line
                if (httpStatus == null)
                {
                    var statusMatch = HttpStatusRegex.Match(content);
                    if (statusMatch.Success)
                        httpStatus = statusMatch.Groups[1].Value;
                }

                // Extract domain error code
                if (errorCode == null)
                {
                    var codeMatch = ErrorCodeRegex.Match(content);
                    if (codeMatch.Success)
                        errorCode = codeMatch.Groups[1].Value;
                }

                // Extract endpoint from "Request starting" line
                if (endpoint == null)
                {
                    var endpointMatch = EndpointRegex.Match(content);
                    if (endpointMatch.Success)
                        endpoint = $"{endpointMatch.Groups[1].Value} {endpointMatch.Groups[2].Value}";
                }

                // Extract error message from WARNING/ERROR entries
                if (errorMessage == null && (level.Equals("Warning", StringComparison.OrdinalIgnoreCase)
                    || level.Equals("Error", StringComparison.OrdinalIgnoreCase)))
                {
                    // Extract the message part after the level indicator
                    var msgPart = Regex.Match(content, @"\]\s*(.+)$");
                    if (msgPart.Success)
                        errorMessage = msgPart.Groups[1].Value.Trim();
                }

                // Track timestamp range
                var tsMatch = TimestampRegex.Match(content);
                if (tsMatch.Success)
                {
                    var ts = tsMatch.Groups[1].Value;
                    firstTimestamp ??= ts;
                    lastTimestamp = ts;
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
