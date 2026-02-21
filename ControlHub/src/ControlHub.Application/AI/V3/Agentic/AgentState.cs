using ControlHub.Application.Common.Interfaces.AI.V3.Agentic;

namespace ControlHub.Application.AI.V3.Agentic
{
    /// <summary>
    /// AgentState implementation - Immutable state object for graph execution.
    /// </summary>
    public class AgentState : IAgentState
    {
        public string CurrentNode { get; set; } = GraphConstants.START;
        public Dictionary<string, object> Context { get; } = new();
        public List<AgentMessage> Messages { get; } = new();
        public bool IsComplete { get; set; }
        public int Iteration { get; set; }
        public int MaxIterations { get; }
        public string? Error { get; set; }

        public AgentState(int maxIterations = 5)
        {
            MaxIterations = maxIterations;
        }

        private AgentState(AgentState source)
        {
            CurrentNode = source.CurrentNode;
            Context = new Dictionary<string, object>(source.Context);
            Messages = new List<AgentMessage>(source.Messages);
            IsComplete = source.IsComplete;
            Iteration = source.Iteration;
            MaxIterations = source.MaxIterations;
            Error = source.Error;
        }

        public IAgentState Clone() => new AgentState(this);

        /// <summary>Add user message</summary>
        public AgentState WithUserMessage(string content)
        {
            var clone = (AgentState)Clone();
            clone.Messages.Add(new AgentMessage("user", content));
            return clone;
        }

        /// <summary>Add assistant message</summary>
        public AgentState WithAssistantMessage(string content)
        {
            var clone = (AgentState)Clone();
            clone.Messages.Add(new AgentMessage("assistant", content));
            return clone;
        }

        /// <summary>Add tool message</summary>
        public AgentState WithToolMessage(string toolName, string content)
        {
            var clone = (AgentState)Clone();
            clone.Messages.Add(new AgentMessage("tool", content, toolName));
            return clone;
        }

        /// <summary>Set context value</summary>
        public AgentState WithContext(string key, object value)
        {
            var clone = (AgentState)Clone();
            clone.Context[key] = value;
            return clone;
        }

        /// <summary>Get typed context value</summary>
        public T? GetContext<T>(string key) where T : class
        {
            return Context.TryGetValue(key, out var value) ? value as T : null;
        }

        /// <summary>Get value type context</summary>
        public T GetContextValue<T>(string key, T defaultValue = default!) where T : struct
        {
            return Context.TryGetValue(key, out var value) && value is T typed ? typed : defaultValue;
        }
    }
}
