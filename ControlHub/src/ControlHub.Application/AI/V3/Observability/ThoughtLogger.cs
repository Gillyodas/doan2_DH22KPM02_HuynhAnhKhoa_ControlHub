using System.Text;
using ControlHub.Application.Common.Interfaces.AI.V3.Agentic;
using Microsoft.Extensions.Logging;

namespace ControlHub.Application.AI.V3.Observability
{
    /// <summary>
    /// ThoughtLogger - Logs agent "thoughts" (reasoning chain) for debugging.
    /// Captures message history and decision points.
    /// </summary>
    public class ThoughtLogger
    {
        private readonly ILogger<ThoughtLogger> _logger;
        private readonly List<ThoughtEntry> _thoughts = new();
        private readonly bool _debugMode;

        public ThoughtLogger(ILogger<ThoughtLogger> logger, bool debugMode = false)
        {
            _logger = logger;
            _debugMode = debugMode;
        }

        /// <summary>
        /// Log a thought/decision point.
        /// </summary>
        public void LogThought(string nodeName, string thought, ThoughtType type = ThoughtType.Reasoning)
        {
            var entry = new ThoughtEntry(
                NodeName: nodeName,
                Thought: thought,
                Type: type,
                Timestamp: DateTime.UtcNow
            );

            _thoughts.Add(entry);

            if (_debugMode)
            {
                var emoji = type switch
                {
                    ThoughtType.Planning => "📋",
                    ThoughtType.Reasoning => "💭",
                    ThoughtType.Action => "⚡",
                    ThoughtType.Observation => "👁",
                    ThoughtType.Reflection => "🔍",
                    _ => "•"
                };
                _logger.LogDebug("{Emoji} [{Node}] {Thought}", emoji, nodeName, thought);
            }
        }

        /// <summary>
        /// Log messages from agent state.
        /// </summary>
        public void LogMessages(IAgentState state)
        {
            foreach (var msg in state.Messages.TakeLast(5))
            {
                var type = msg.Role switch
                {
                    "user" => ThoughtType.Observation,
                    "assistant" => ThoughtType.Reasoning,
                    "tool" => ThoughtType.Action,
                    _ => ThoughtType.Observation
                };

                LogThought(state.CurrentNode, $"[{msg.Role}] {msg.Content}", type);
            }
        }

        /// <summary>
        /// Get formatted thought chain for display.
        /// </summary>
        public string GetThoughtChain()
        {
            var sb = new StringBuilder();
            sb.AppendLine("=== Agent Thought Chain ===");

            var grouped = _thoughts.GroupBy(t => t.NodeName);
            foreach (var group in grouped)
            {
                sb.AppendLine($"\n[{group.Key}]");
                foreach (var thought in group)
                {
                    var prefix = thought.Type switch
                    {
                        ThoughtType.Planning => "PLAN:",
                        ThoughtType.Reasoning => "THINK:",
                        ThoughtType.Action => "ACT:",
                        ThoughtType.Observation => "OBS:",
                        ThoughtType.Reflection => "REFLECT:",
                        _ => ""
                    };
                    sb.AppendLine($"  {prefix} {thought.Thought}");
                }
            }

            return sb.ToString();
        }

        /// <summary>
        /// Get ReAct-style formatted output.
        /// </summary>
        public string GetReActFormat()
        {
            var sb = new StringBuilder();
            foreach (var thought in _thoughts)
            {
                var line = thought.Type switch
                {
                    ThoughtType.Planning => $"Thought: {thought.Thought}",
                    ThoughtType.Action => $"Action: {thought.Thought}",
                    ThoughtType.Observation => $"Observation: {thought.Thought}",
                    ThoughtType.Reflection => $"Reflection: {thought.Thought}",
                    _ => $"Thought: {thought.Thought}"
                };
                sb.AppendLine(line);
            }
            return sb.ToString();
        }

        public IReadOnlyList<ThoughtEntry> Thoughts => _thoughts;
    }

    /// <summary>
    /// A thought/decision entry.
    /// </summary>
    public record ThoughtEntry(
        string NodeName,
        string Thought,
        ThoughtType Type,
        DateTime Timestamp
    );

    /// <summary>
    /// Types of thoughts (ReAct-style).
    /// </summary>
    public enum ThoughtType
    {
        Planning,
        Reasoning,
        Action,
        Observation,
        Reflection
    }
}
