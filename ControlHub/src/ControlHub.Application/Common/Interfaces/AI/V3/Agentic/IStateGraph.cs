namespace ControlHub.Application.Common.Interfaces.AI.V3.Agentic
{
    /// <summary>
    /// State Graph interface - LangGraph-inspired workflow orchestration.
    /// Nodes are connected by edges (optionally conditional).
    /// </summary>
    public interface IStateGraph
    {
        /// <summary>Add node to graph</summary>
        void AddNode(IAgentNode node);

        /// <summary>
        /// Add edge between nodes.
        /// </summary>
        /// <param name="from">Source node name (or START for entry point)</param>
        /// <param name="to">Target node name (or END for termination)</param>
        /// <param name="condition">Optional condition function. If null, always transition.</param>
        void AddEdge(string from, string to, Func<IAgentState, bool>? condition = null);

        /// <summary>
        /// Add conditional edges (multiple targets based on state).
        /// </summary>
        void AddConditionalEdges(string from, Func<IAgentState, string> router);

        /// <summary>
        /// Run the graph from START to END.
        /// </summary>
        /// <param name="initialState">Starting state</param>
        /// <param name="ct">Cancellation token</param>
        /// <returns>Final state after graph execution</returns>
        Task<IAgentState> RunAsync(IAgentState initialState, CancellationToken ct = default);

        /// <summary>
        /// Get all registered nodes.
        /// </summary>
        IReadOnlyDictionary<string, IAgentNode> Nodes { get; }
    }

    /// <summary>
    /// Special node names for entry/exit.
    /// </summary>
    public static class GraphConstants
    {
        public const string START = "__START__";
        public const string END = "__END__";
    }
}
