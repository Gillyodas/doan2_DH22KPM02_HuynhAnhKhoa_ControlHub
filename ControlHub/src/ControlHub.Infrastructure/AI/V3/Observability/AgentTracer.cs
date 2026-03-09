using System.Diagnostics;
using ControlHub.Application.AuditAI.Interfaces.V3.Agentic;
using ControlHub.Application.AuditAI.Interfaces.V3.Observability;
using Microsoft.Extensions.Logging;

namespace ControlHub.Infrastructure.AI.V3.Observability
{
    public class AgentTracer : IAgentObserver
    {
        private readonly ILogger<AgentTracer> _logger;
        private readonly List<AgentEvent> _events = new();
        private readonly ActivitySource _activitySource;

        public AgentTracer(ILogger<AgentTracer> logger)
        {
            _logger = logger;
            _activitySource = new ActivitySource("ControlHub.AuditAgent.V3");
        }

        public IReadOnlyList<AgentEvent> Events => _events;

        public Task OnNodeStarted(string nodeName, IAgentState state)
        {
            var evt = new AgentEvent(
                Type: AgentEventType.NodeStarted,
                NodeName: nodeName,
                Message: $"Node '{nodeName}' started (iteration {state.Iteration})",
                DurationMs: null,
                Timestamp: DateTime.UtcNow,
                Data: new Dictionary<string, object>
                {
                    ["iteration"] = state.Iteration,
                    ["messageCount"] = state.Messages.Count
                }
            );

            _events.Add(evt);
            _logger.LogInformation("{Message}", evt.Message);

            using var activity = _activitySource.StartActivity($"Node.{nodeName}");
            activity?.SetTag("node.name", nodeName);
            activity?.SetTag("state.iteration", state.Iteration);

            return Task.CompletedTask;
        }

        public Task OnNodeCompleted(string nodeName, IAgentState state, TimeSpan duration)
        {
            var evt = new AgentEvent(
                Type: AgentEventType.NodeCompleted,
                NodeName: nodeName,
                Message: $"Node '{nodeName}' completed in {duration.TotalMilliseconds:F0}ms",
                DurationMs: (long)duration.TotalMilliseconds,
                Timestamp: DateTime.UtcNow,
                Data: new Dictionary<string, object>
                {
                    ["iteration"] = state.Iteration,
                    ["hasError"] = state.Error != null
                }
            );

            _events.Add(evt);
            _logger.LogInformation("{Message}", evt.Message);

            return Task.CompletedTask;
        }

        public Task OnNodeFailed(string nodeName, IAgentState state, Exception error)
        {
            var evt = new AgentEvent(
                Type: AgentEventType.NodeFailed,
                NodeName: nodeName,
                Message: $"Node '{nodeName}' failed: {error.Message}",
                DurationMs: null,
                Timestamp: DateTime.UtcNow,
                Data: new Dictionary<string, object>
                {
                    ["errorType"] = error.GetType().Name,
                    ["stackTrace"] = error.StackTrace ?? ""
                }
            );

            _events.Add(evt);
            _logger.LogError(error, "{Message}", evt.Message);

            return Task.CompletedTask;
        }

        public Task OnStateChanged(IAgentState oldState, IAgentState newState)
        {
            var evt = new AgentEvent(
                Type: AgentEventType.StateChanged,
                NodeName: newState.CurrentNode,
                Message: $"State changed: {oldState.CurrentNode} -> {newState.CurrentNode}",
                DurationMs: null,
                Timestamp: DateTime.UtcNow,
                Data: new Dictionary<string, object>
                {
                    ["from"] = oldState.CurrentNode,
                    ["to"] = newState.CurrentNode
                }
            );

            _events.Add(evt);
            _logger.LogDebug("{Message}", evt.Message);

            return Task.CompletedTask;
        }

        public Task OnGraphCompleted(IAgentState finalState, TimeSpan totalDuration)
        {
            var evt = new AgentEvent(
                Type: AgentEventType.GraphCompleted,
                NodeName: null,
                Message: $"Graph completed in {totalDuration.TotalMilliseconds:F0}ms ({finalState.Iteration} iterations)",
                DurationMs: (long)totalDuration.TotalMilliseconds,
                Timestamp: DateTime.UtcNow,
                Data: new Dictionary<string, object>
                {
                    ["iterations"] = finalState.Iteration,
                    ["isComplete"] = finalState.IsComplete,
                    ["hasError"] = finalState.Error != null
                }
            );

            _events.Add(evt);
            _logger.LogInformation("{Message}", evt.Message);

            return Task.CompletedTask;
        }
    }
}
