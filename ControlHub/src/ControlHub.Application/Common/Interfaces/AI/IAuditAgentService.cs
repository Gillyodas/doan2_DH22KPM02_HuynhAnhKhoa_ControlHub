using System.Collections.Generic;
using System.Threading.Tasks;

namespace ControlHub.Application.Common.Interfaces.AI
{
    public interface IAuditAgentService
    {
        Task<AuditResult> InvestigateSessionAsync(string correlationId, string lang = "en");
        
        /// <summary>
        /// V2.5 Chat with agent workflow (Drain3 + Sampling + Runbooks).
        /// </summary>
        Task<ChatResult> ChatAsync(ChatRequest request, string lang = "en");
    }

    public record AuditResult(
        string Analysis, 
        List<LogTemplate> ProcessedTemplates, 
        List<string> ToolsUsed
    );

    /// <summary>
    /// Result from Agentic Chat operation.
    /// </summary>
    public record ChatResult(
        string Answer,
        int LogCount,
        List<string> ToolsUsed
    );

    /// <summary>
    /// Request DTO for Chat endpoint.
    /// </summary>
    public class ChatRequest
    {
        public string Question { get; set; } = string.Empty;
        public DateTime? StartTime { get; set; }
        public DateTime? EndTime { get; set; }
        public string? CorrelationId { get; set; }
    }
}
