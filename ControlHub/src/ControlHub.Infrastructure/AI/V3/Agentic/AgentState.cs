using ControlHub.Application.AuditAI.Interfaces.V3.Agentic;

namespace ControlHub.Infrastructure.AI.V3.Agentic
{
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

        public T? GetContext<T>(string key) where T : class
        {
            return Context.TryGetValue(key, out var value) ? value as T : null;
        }

        public T GetContextValue<T>(string key, T defaultValue = default!) where T : struct
        {
            return Context.TryGetValue(key, out var value) && value is T typed ? typed : defaultValue;
        }
    }
}
