namespace ControlHub.Application.Common.Interfaces.AI
{
    public interface IRunbookService
    {
        Task IngestRunbooksAsync(IEnumerable<RunbookEntry> runbooks);
        Task<List<RunbookEntry>> FindRelatedRunbooksAsync(string logCodeOrPattern, int limit = 3);
    }

    public record RunbookEntry(string LogCode, string Problem, string Solution, string[] Tags);
}
