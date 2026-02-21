namespace ControlHub.Application.Common.Interfaces.AI
{
    public interface ISamplingStrategy
    {
        List<LogTemplate> Sample(List<LogTemplate> templates, int maxCount = 50);
    }
}
