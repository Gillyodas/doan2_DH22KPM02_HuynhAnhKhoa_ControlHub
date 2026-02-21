using ControlHub.Application.Common.Logging;
namespace ControlHub.Application.Common.Interfaces.AI.V3.Parsing
{
    /// <summary>
    /// Hybrid parser k?t h?p Drain3 (fast, rule-based) và Semantic Classifier (slow, ML-based).
    /// Strategy: Dùng Drain3 tru?c, n?u confidence th?p thì dùng Semantic Classifier.
    /// </summary>
    public interface IHybridLogParser
    {
        /// <summary>
        /// Parse danh sách logs v?i hybrid strategy.
        /// </summary>
        /// <param name="logs">Raw log entries</param>
        /// <param name="options">Parsing options (confidence threshold, fallback behavior)</param>
        /// <param name="ct">Cancellation token</param>
        /// <returns>Parsed result v?i templates và metadata</returns>
        Task<HybridParseResult> ParseLogsAsync(
            List<LogEntry> logs,
            HybridParsingOptions? options = null,
            CancellationToken ct = default
        );

        /// <summary>
        /// Parse m?t log line don l? (dùng cho real-time processing).
        /// </summary>
        Task<ParsedLog> ParseSingleAsync(string logLine, CancellationToken ct = default);
    }
    /// <summary>
    /// K?t qu? parsing v?i metadata v? strategy du?c dùng.
    /// </summary>
    public record HybridParseResult(
        /// <summary>Danh sách templates (gi?ng V2.5)</summary>
        List<LogTemplate> Templates,

        /// <summary>Mapping t? template ID ? raw logs</summary>
        Dictionary<string, List<LogEntry>> TemplateToLogs,

        /// <summary>Metadata v? parsing strategy</summary>
        ParsingMetadata Metadata
    );
    /// <summary>
    /// Metadata v? quá trình parsing.
    /// </summary>
    public record ParsingMetadata(
        /// <summary>S? logs du?c parse b?i Drain3</summary>
        int Drain3Count,

        /// <summary>S? logs du?c parse b?i Semantic Classifier</summary>
        int SemanticCount,

        /// <summary>S? logs failed (không parse du?c)</summary>
        int FailedCount,

        /// <summary>Average confidence score</summary>
        float AverageConfidence,

        /// <summary>Th?i gian x? lý (milliseconds)</summary>
        long ProcessingTimeMs
    );
    /// <summary>
    /// K?t qu? parse m?t log line.
    /// </summary>
    public record ParsedLog(
        string OriginalLine,
        string Template,
        LogClassification? Classification,
        ParsingMethod Method,
        float Confidence
    );
    /// <summary>
    /// Method du?c dùng d? parse log.
    /// </summary>
    public enum ParsingMethod
    {
        Drain3,
        Semantic,
        Failed
    }
    /// <summary>
    /// Options cho hybrid parsing.
    /// </summary>
    public record HybridParsingOptions(
        /// <summary>Confidence threshold d? fallback sang Semantic (default: 0.7)</summary>
        float ConfidenceThreshold = 0.7f,

        /// <summary>Có enable Semantic Classifier không (default: true)</summary>
        bool EnableSemantic = true,

        /// <summary>Có enable Drain3 không (default: true)</summary>
        bool EnableDrain3 = true,

        /// <summary>Max logs d? x? lý b?ng Semantic (tránh quá ch?m, default: 100)</summary>
        int MaxSemanticLogs = 100
    );
}
