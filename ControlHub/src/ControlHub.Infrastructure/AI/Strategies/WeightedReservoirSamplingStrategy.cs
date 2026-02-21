using ControlHub.Application.Common.Interfaces.AI;

namespace ControlHub.Infrastructure.AI.Strategies
{
    public class WeightedReservoirSamplingStrategy : ISamplingStrategy
    {
        public List<LogTemplate> Sample(List<LogTemplate> templates, int maxCount = 50)
        {
            if (templates.Count <= maxCount) return templates.OrderBy(t => t.FirstSeen).ToList();

            // Calculate weights: w = SeverityWeight * (1 / log(Count + 1))
            // We want rare errors to be kept.

            var weightedItems = templates.Select(t => new
            {
                Template = t,
                Weight = CalculateWeight(t)
            }).ToList();

            // Algorithm A-Res (Efraimidis & Spirakis)
            // Generate a random key k = u^(1/w) where u is uniform random (0,1)
            var random = new Random();
            var sampled = weightedItems
                .Select(x => new
                {
                    x.Template,
                    Key = Math.Pow(random.NextDouble(), 1.0 / x.Weight)
                })
                .OrderByDescending(x => x.Key)
                .Take(maxCount)
                .Select(x => x.Template)
                .OrderBy(t => t.FirstSeen) // Re-order chronologically for context
                .ToList();

            return sampled;
        }

        private double CalculateWeight(LogTemplate template)
        {
            double severityFactor = template.Severity?.ToLower() switch
            {
                "fatal" => 100.0,
                "error" => 50.0,
                "warning" => 10.0,
                _ => 1.0
            };

            // Inverse Term Frequency (ITF) like approach
            // +1 to avoid division by zero (though Count >= 1)
            // We use standard log10
            double countFactor = 1.0 / Math.Log10(template.Count + 10); // +10 to smooth out very rare items domination? 
                                                                        // Original formula: IDF. Here let's just say:
                                                                        // Rare items (low count) -> High factor

            // Refined: Severity matters most. Then rarity.
            // If count is 1: 1/log(1+1) = 3.32
            // If count is 1000: 1/log(1001) = 0.33

            return severityFactor * (1.0 / Math.Log(template.Count + 1));
        }
    }
}
