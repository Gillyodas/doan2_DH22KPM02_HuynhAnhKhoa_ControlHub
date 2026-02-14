namespace ControlHub.Application.Common.Interfaces.AI.V3
{
    /// <summary>
    /// Provides system-level domain knowledge for AI investigation context.
    /// Maps error codes to module rules for accurate diagnosis.
    /// </summary>
    public interface ISystemKnowledgeProvider
    {
        /// <summary>
        /// Get domain knowledge relevant to a specific error code.
        /// </summary>
        /// <param name="errorCode">Domain error code (e.g., "Permission.InvalidFormat")</param>
        /// <returns>Formatted knowledge string for prompt injection, or empty if no match</returns>
        string GetKnowledgeForErrorCode(string errorCode);

        /// <summary>
        /// Get the full system architecture context.
        /// </summary>
        /// <returns>Formatted architecture overview for prompt injection</returns>
        string GetFullSystemContext();
    }
}
