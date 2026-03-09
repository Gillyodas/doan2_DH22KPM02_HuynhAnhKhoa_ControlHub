using ControlHub.Application.AuditAI.Logging;

namespace ControlHub.Application.AuditAI.Interfaces.V1
{
    public interface ILogKnowledgeService
    {
        Task IngestLogDefinitionsAsync();
        Task<string> AnalyzeSessionAsync(List<LogEntry> logs, string lang = "en");
        Task<string> ChatWithLogsAsync(string userQuestion, List<LogEntry> logs, string lang = "en");
    }
}
