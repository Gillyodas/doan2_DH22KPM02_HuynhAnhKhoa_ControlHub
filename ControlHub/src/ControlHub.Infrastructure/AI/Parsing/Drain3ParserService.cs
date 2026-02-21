using System.Text.RegularExpressions;
using ControlHub.Application.Common.Interfaces.AI;
using ControlHub.Application.Common.Logging;

namespace ControlHub.Infrastructure.AI.Parsing
{
    public class Drain3ParserService : ILogParserService
    {
        private readonly Node _root;
        private readonly int _depth;
        private readonly double _similarityThreshold;
        private readonly List<Cluster> _clusters;

        // Pre-compiled regex for masking
        private static readonly Regex IpRegex = new Regex(@"(?<!\d)(?:(?:25[0-5]|2[0-4][0-9]|[01]?[0-9][0-9]?)\.){3}(?:25[0-5]|2[0-4][0-9]|[01]?[0-9][0-9]?)(?!\d)", RegexOptions.Compiled);
        private static readonly Regex GuidRegex = new Regex(@"[0-9a-fA-F]{8}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{12}", RegexOptions.Compiled);
        private static readonly Regex NumberRegex = new Regex(@"(?<!\w)-?\d+(?:\.\d+)?(?!\w)", RegexOptions.Compiled);

        public Drain3ParserService(int depth = 4, double similarityThreshold = 0.5)
        {
            _root = new Node();
            _depth = depth;
            _similarityThreshold = similarityThreshold;
            _clusters = new List<Cluster>();
        }

        public Task<LogParseResult> ParseLogsAsync(List<LogEntry> rawLogs)
        {
            // Reset state for per-session analysis (or keep persistent if singleton)
            // For now, we assume per-request scope or we want fresh analysis per audit session.
            // If Scoped, this is fine.

            var templateToLogs = new Dictionary<string, List<LogEntry>>();

            foreach (var log in rawLogs)
            {
                if (string.IsNullOrWhiteSpace(log.Message)) continue;

                var (content, matchCluster) = ProcessLogLine(log.Message);

                // Add log to result mapping
                if (!templateToLogs.ContainsKey(matchCluster.TemplateId))
                {
                    templateToLogs[matchCluster.TemplateId] = new List<LogEntry>();
                }
                templateToLogs[matchCluster.TemplateId].Add(log);

                // Update stats
                matchCluster.Count++;
                if (log.Timestamp < matchCluster.FirstSeen) matchCluster.FirstSeen = log.Timestamp;
                if (log.Timestamp > matchCluster.LastSeen) matchCluster.LastSeen = log.Timestamp;

                // Aggregate Severity (Simple heuristic: highest severity wins)
                if (GetSeverityWeight(log.Level) > GetSeverityWeight(matchCluster.Severity))
                {
                    matchCluster.Severity = log.Level;
                }
            }

            var templates = _clusters.Select(c => new LogTemplate(
                c.TemplateId,
                string.Join(" ", c.LogTemplateTokens),
                c.Count,
                c.FirstSeen,
                c.LastSeen,
                c.Severity
            )).ToList();

            return Task.FromResult(new LogParseResult(templates, templateToLogs));
        }

        private (string Content, Cluster Match) ProcessLogLine(string logLine)
        {
            // 1. Masking
            var content = Mask(logLine);

            // 2. Tokenize
            var tokens = content.Split(new[] { ' ', '\t', ',', ':', ';', '=', '[', ']' }, StringSplitOptions.RemoveEmptyEntries);

            // 3. Tree Search
            var matchCluster = TreeSearch(_root, tokens);

            if (matchCluster == null)
            {
                // Create new cluster and add to tree
                matchCluster = new Cluster
                {
                    TemplateId = Guid.NewGuid().ToString("N").Substring(0, 8),
                    LogTemplateTokens = tokens.ToList(),
                    FirstSeen = DateTime.MaxValue,
                    LastSeen = DateTime.MinValue,
                    Severity = "Information"
                };
                AddCluster(_root, matchCluster, tokens);
                _clusters.Add(matchCluster);
            }
            else
            {
                // Update template
                UpdateTemplate(matchCluster, tokens);
            }

            return (content, matchCluster);
        }

        private string Mask(string logLine)
        {
            logLine = IpRegex.Replace(logLine, "<IP>");
            logLine = GuidRegex.Replace(logLine, "<GUID>");
            logLine = NumberRegex.Replace(logLine, "<NUM>");
            return logLine;
        }

        private Cluster? TreeSearch(Node root, string[] tokens)
        {
            var length = tokens.Length;
            if (!root.Children.TryGetValue(length.ToString(), out var lengthNode))
            {
                return null;
            }

            Node current = lengthNode;
            int currentDepth = 1;

            // Traverse down to leaf
            foreach (var token in tokens)
            {
                if (currentDepth >= _depth) break;

                if (current.Children.TryGetValue(token, out var nextNode))
                {
                    current = nextNode;
                }
                else if (current.Children.TryGetValue("*", out var wildcardNode))
                {
                    current = wildcardNode;
                }
                else
                {
                    // No path found in internal nodes
                    // However, Drain3 usually tries to match all candidates in the leaf node
                    break;
                }
                currentDepth++;
            }

            // Find best match in cluster list of the node
            double maxSim = -1;
            Cluster? bestMatch = null;

            foreach (var cluster in current.Clusters)
            {
                double sim = GetSeqSimilarity(cluster.LogTemplateTokens, tokens);
                if (sim > maxSim)
                {
                    maxSim = sim;
                    bestMatch = cluster;
                }
            }

            if (maxSim >= _similarityThreshold)
            {
                return bestMatch;
            }

            return null;
        }

        private void AddCluster(Node root, Cluster cluster, string[] tokens)
        {
            var length = tokens.Length.ToString();
            if (!root.Children.ContainsKey(length))
            {
                root.Children[length] = new Node();
            }

            Node current = root.Children[length];
            int currentDepth = 1;

            foreach (var token in tokens)
            {
                if (currentDepth >= _depth) break;

                // Simple strategy: exact match or create new path
                // Drain3 has smarter internal node construction (e.g. wildcard creation), 
                // but for simplicity we assume fixed paths until leaf.
                // Improvement: If node has too many children, collapse to "*"

                if (!current.Children.ContainsKey(token))
                {
                    if (current.Children.Count > 5) // Max branching factor heuristic
                    {
                        if (!current.Children.ContainsKey("*"))
                            current.Children["*"] = new Node();
                        current = current.Children["*"];
                    }
                    else
                    {
                        current.Children[token] = new Node();
                        current = current.Children[token];
                    }
                }
                else
                {
                    current = current.Children[token];
                }
                currentDepth++;
            }

            current.Clusters.Add(cluster);
        }

        private void UpdateTemplate(Cluster cluster, string[] tokens)
        {
            // Simple update: if token mismatch, change to <*>
            for (int i = 0; i < Math.Min(cluster.LogTemplateTokens.Count, tokens.Length); i++)
            {
                if (cluster.LogTemplateTokens[i] != tokens[i])
                {
                    cluster.LogTemplateTokens[i] = "<*>";
                }
            }
        }

        private double GetSeqSimilarity(List<string> template, string[] log)
        {
            if (template.Count != log.Length) return 0;

            int match = 0;
            for (int i = 0; i < template.Count; i++)
            {
                if (template[i] == log[i] || template[i] == "<*>")
                {
                    match++;
                }
            }
            return (double)match / template.Count;
        }

        private int GetSeverityWeight(string level)
        {
            return level?.ToLower() switch
            {
                "fatal" => 5,
                "error" => 4,
                "warning" => 3,
                "information" => 2,
                "debug" => 1,
                _ => 1
            };
        }

        // Inner classes
        private class Node
        {
            public Dictionary<string, Node> Children { get; } = new Dictionary<string, Node>();
            public List<Cluster> Clusters { get; } = new List<Cluster>();
        }

        private class Cluster
        {
            public string TemplateId { get; set; } = string.Empty;
            public List<string> LogTemplateTokens { get; set; } = new List<string>();
            public int Count { get; set; } = 0;
            public DateTime FirstSeen { get; set; }
            public DateTime LastSeen { get; set; }
            public string Severity { get; set; } = "Information";
        }
    }
}
