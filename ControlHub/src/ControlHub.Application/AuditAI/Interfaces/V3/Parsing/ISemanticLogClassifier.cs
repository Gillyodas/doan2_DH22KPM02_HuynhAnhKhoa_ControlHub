namespace ControlHub.Application.AuditAI.Interfaces.V3.Parsing
{
    /// <summary>
    /// Semantic log classifier s? d?ng ML model d? phân lo?i log d?a trên ng? nghia.
    /// Khác v?i Drain3 (rule-based pattern matching), classifier này hi?u "ý nghia" c?a log.
    /// </summary>
    public interface ISemanticLogClassifier
    {
        /// <summary>
        /// Phân lo?i m?t dòng log thành category/subcategory.
        /// </summary>
        /// <param name="logLine">Raw log line (ví d?: "User authentication failed: Invalid password")</param>
        /// <param name="ct">Cancellation token</param>
        /// <returns>LogClassification v?i category, confidence score, và extracted fields</returns>
        Task<LogClassification> ClassifyAsync(string logLine, CancellationToken ct = default);

        /// <summary>
        /// Tính confidence score cho m?t category c? th?.
        /// Dùng d? verify prediction ho?c so sánh v?i threshold.
        /// </summary>
        /// <param name="logLine">Raw log line</param>
        /// <param name="expectedCategory">Category c?n ki?m tra (ví d?: "auth_failure")</param>
        /// <param name="ct">Cancellation token</param>
        /// <returns>Confidence score t? 0.0 d?n 1.0</returns>
        Task<float> GetConfidenceAsync(string logLine, string expectedCategory, CancellationToken ct = default);
    }
    /// <summary>
    /// K?t qu? phân lo?i log.
    /// </summary>
    public record LogClassification(
        /// <summary>Category chính (ví d?: "authentication", "database", "network")</summary>
        string Category,

        /// <summary>SubCategory chi ti?t (ví d?: "mfa_timeout", "connection_pool_exhausted")</summary>
        string SubCategory,

        /// <summary>Confidence score (0.0 - 1.0). N?u < 0.7 nên fallback sang Drain3</summary>
        float Confidence,

        /// <summary>
        /// Các fields du?c extract t? log (ví d?: {"user": "admin@corp.com", "ip": "192.168.1.1"})
        /// </summary>
        Dictionary<string, string> ExtractedFields
    );
}
