namespace ControlHub.Application.Common.Interfaces.AI.V3.RAG
{
    /// <summary>
    /// Processes raw log entries into structured evidence for LLM consumption.
    /// Filters noise, extracts metadata, and formats a summary.
    /// </summary>
    public interface ILogEvidenceProcessor
    {
        /// <summary>
        /// Process raw log documents into prioritized, structured evidence.
        /// </summary>
        /// <param name="rawLogs">Raw log documents from LogReader</param>
        /// <returns>Processed evidence with metadata and formatted summary</returns>
        EvidenceSummary ProcessLogs(IReadOnlyList<RetrievedDocument> rawLogs);
    }

    /// <summary>
    /// Result of log evidence processing.
    /// </summary>
    public record EvidenceSummary(
        /// <summary>Logs sorted by priority: ERROR → WARNING → filtered INFO</summary>
        List<RetrievedDocument> PrioritizedLogs,

        /// <summary>Auto-extracted metadata (HTTP status, error codes, etc.)</summary>
        LogMetadata ExtractedMetadata,

        /// <summary>Pre-formatted text summary for LLM prompt injection</summary>
        string FormattedSummary
    );

    /// <summary>
    /// Metadata extracted from log entries using regex (code-based, 100% accurate).
    /// </summary>
    public record LogMetadata(
        /// <summary>HTTP status code from "Request finished" log (e.g., "400", "500")</summary>
        string? HttpStatusCode,

        /// <summary>Domain error code (e.g., "Permission.InvalidFormat")</summary>
        string? ErrorCode,

        /// <summary>Affected HTTP endpoint (e.g., "POST /api/Permission/permissions")</summary>
        string? AffectedEndpoint,

        /// <summary>Primary error message from WARNING/ERROR entries</summary>
        string? ErrorMessage,

        /// <summary>Time range of the request (e.g., "10:15:03 – 10:15:04")</summary>
        string? TimestampRange,

        /// <summary>Count of ERROR level entries</summary>
        int ErrorCount = 0,

        /// <summary>Count of WARNING level entries</summary>
        int WarningCount = 0,

        /// <summary>Count of INFO level entries</summary>
        int InfoCount = 0
    );
}
