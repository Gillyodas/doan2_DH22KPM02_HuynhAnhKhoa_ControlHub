using ControlHub.Application.Common.Logging;

namespace ControlHub.Application.Common.Interfaces.AI
{
    public interface ILogParserService
    {
        Task<LogParseResult> ParseLogsAsync(List<LogEntry> rawLogs);
    }

    public record LogTemplate(
        string TemplateId,
        string Pattern,
        int Count,
        DateTime FirstSeen,
        DateTime LastSeen,
        string Severity
    );

    public record LogParseResult(
        List<LogTemplate> Templates,
        Dictionary<string, List<LogEntry>> TemplateToLogs
    );
}
