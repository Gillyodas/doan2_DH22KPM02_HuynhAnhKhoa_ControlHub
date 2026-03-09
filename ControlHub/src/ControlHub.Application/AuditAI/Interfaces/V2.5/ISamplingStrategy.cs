namespace ControlHub.Application.AuditAI.Interfaces
{
    public interface ISamplingStrategy
    {
        List<LogTemplate> Sample(List<LogTemplate> templates, int maxCount = 50);
    }
}
