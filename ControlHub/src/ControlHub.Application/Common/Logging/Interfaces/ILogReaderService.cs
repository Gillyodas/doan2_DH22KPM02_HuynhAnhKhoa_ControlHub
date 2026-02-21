namespace ControlHub.Application.Common.Logging.Interfaces
{
    public interface ILogReaderService
    {
        Task<List<LogEntry>> GetRecentLogsAsync(int count = 100);
        Task<List<LogEntry>> GetLogsByCorrelationIdAsync(string correlationId);
        Task<List<LogEntry>> GetLogsByTimeRangeAsync(DateTime startTime, DateTime endTime);
    }
}
