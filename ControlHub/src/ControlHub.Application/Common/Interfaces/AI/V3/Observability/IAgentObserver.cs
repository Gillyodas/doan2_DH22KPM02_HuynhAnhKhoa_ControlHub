using ControlHub.Application.Common.Interfaces.AI.V3.Agentic;

namespace ControlHub.Application.Common.Interfaces.AI.V3.Observability
{
    /// <summary>
    /// Agent Observer interface - Observability hooks for agent execution.
    /// Enables tracing, logging, and monitoring.
    /// </summary>
    public interface IAgentObserver
    {
        /// <summary>Called when a node starts execution</summary>
        Task OnNodeStarted(string nodeName, IAgentState state);

        /// <summary>Called when a node completes successfully</summary>
        Task OnNodeCompleted(string nodeName, IAgentState state, TimeSpan duration);

        /// <summary>Called when a node fails</summary>
        Task OnNodeFailed(string nodeName, IAgentState state, Exception error);

        /// <summary>Called when state changes</summary>
        Task OnStateChanged(IAgentState oldState, IAgentState newState);

        /// <summary>Called when graph execution completes</summary>
        Task OnGraphCompleted(IAgentState finalState, TimeSpan totalDuration);
    }

    /// <summary>
    /// Agent execution event for structured logging.
    /// </summary>
    public record AgentEvent(
        /// <summary>Event type</summary>
        AgentEventType Type,

        /// <summary>Node name (if applicable)</summary>
        string? NodeName,

        /// <summary>Event message</summary>
        string Message,

        /// <summary>Duration in milliseconds</summary>
        long? DurationMs,

        /// <summary>Timestamp</summary>
        DateTime Timestamp,

        /// <summary>Additional data</summary>
        System.Collections.Generic.Dictionary<string, object>? Data = null
    );

    /// <summary>
    /// Event types for agent execution.
    /// </summary>
    public enum AgentEventType
    {
        GraphStarted,
        GraphCompleted,
        GraphFailed,
        NodeStarted,
        NodeCompleted,
        NodeFailed,
        StateChanged,
        ToolCalled,
        ReflexionTriggered
    }
}
