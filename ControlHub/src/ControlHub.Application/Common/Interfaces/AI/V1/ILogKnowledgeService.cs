using ControlHub.Application.Common.Logging;

namespace ControlHub.Application.Common.Interfaces.AI.V1
{
    public interface ILogKnowledgeService
    {
        Task IngestLogDefinitionsAsync();
        Task<string> AnalyzeSessionAsync(List<LogEntry> logs, string lang = "en");
        Task<string> ChatWithLogsAsync(string userQuestion, List<LogEntry> logs, string lang = "en");
    }
}
