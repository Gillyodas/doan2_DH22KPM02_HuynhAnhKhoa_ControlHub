namespace ControlHub.Application.Common.Logging.Interfaces.AI
{
    public interface IAIAnalysisService
    {
        Task<string> AnalyzeLogsAsync(IEnumerable<LogEntry> logs);
    }
}
