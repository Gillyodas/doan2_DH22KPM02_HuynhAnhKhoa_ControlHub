using ControlHub.Application.Common.Interfaces.AI;

namespace ControlHub.Infrastructure.AI.Strategies
{
    public class NaiveSamplingStrategy : ISamplingStrategy
    {
        public List<LogTemplate> Sample(List<LogTemplate> templates, int maxCount = 50)
        {
            // Simple TakeLast
            return templates
                .OrderBy(t => t.LastSeen) // Chronological
                .TakeLast(maxCount)
                .ToList();
        }
    }
}
