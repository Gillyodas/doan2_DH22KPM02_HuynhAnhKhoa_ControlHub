using ControlHub.Application.Common.Interfaces.AI.V3.Agentic;
using ControlHub.Application.Common.Interfaces.AI.V3.Observability;
using Microsoft.Extensions.Logging;

namespace ControlHub.Application.AI.V3.Agentic
{
    /// <summary>
    /// StateGraph implementation - LangGraph-inspired workflow orchestration.
    /// Executes nodes in sequence following edges.
    /// </summary>
    public class StateGraph : IStateGraph
    {
        private readonly IAgentObserver? _observer;
        private readonly ILogger<StateGraph> _logger;
        private readonly Dictionary<string, IAgentNode> _nodes = new();
        private readonly Dictionary<string, List<Edge>> _edges = new();
        private readonly Dictionary<string, Func<IAgentState, string>> _conditionalRouters = new();

        public IReadOnlyDictionary<string, IAgentNode> Nodes => _nodes;

        public StateGraph(ILogger<StateGraph> logger, IAgentObserver? observer = null)
        {
            _logger = logger;
            _observer = observer;
        }

        public void AddNode(IAgentNode node)
        {
            if (_nodes.ContainsKey(node.Name))
                throw new InvalidOperationException($"Node '{node.Name}' already exists");

            _nodes[node.Name] = node;
            _logger.LogDebug("Added node: {NodeName}", node.Name);
        }

        public void AddEdge(string from, string to, Func<IAgentState, bool>? condition = null)
        {
            if (!_edges.ContainsKey(from))
                _edges[from] = new List<Edge>();

            _edges[from].Add(new Edge(to, condition));
            _logger.LogDebug("Added edge: {From} -> {To}", from, to);
        }

        public void AddConditionalEdges(string from, Func<IAgentState, string> router)
        {
            _conditionalRouters[from] = router;
            _logger.LogDebug("Added conditional router for: {From}", from);
        }

        public async Task<IAgentState> RunAsync(IAgentState initialState, CancellationToken ct = default)
        {
            var state = initialState.Clone();
            state.CurrentNode = GraphConstants.START;

            var graphStopwatch = System.Diagnostics.Stopwatch.StartNew();
            _logger.LogInformation("Graph execution started");

            while (!state.IsComplete && !ct.IsCancellationRequested)
            {
                // Check iteration limit
                if (state.Iteration >= state.MaxIterations)
                {
                    _logger.LogWarning("Max iterations ({Max}) reached", state.MaxIterations);
                    state.Error = "Max iterations reached";
                    state.IsComplete = true;
                    break;
                }

                // Get next node
                var nextNode = GetNextNode(state);
                if (nextNode == null || nextNode == GraphConstants.END)
                {
                    _logger.LogInformation("Reached END node");
                    state.IsComplete = true;
                    break;
                }

                // Execute node
                state.CurrentNode = nextNode;
                state.Iteration++;

                if (!_nodes.TryGetValue(nextNode, out var node))
                {
                    _logger.LogError("Node '{NodeName}' not found", nextNode);
                    state.Error = $"Node '{nextNode}' not found";
                    state.IsComplete = true;
                    break;
                }

                _logger.LogInformation("Executing node: {NodeName} (iteration {Iter})",
                    node.Name, state.Iteration);

                if (_observer != null)
                    await _observer.OnNodeStarted(node.Name, state);

                var sw = System.Diagnostics.Stopwatch.StartNew();
                try
                {
                    var oldState = state.Clone();
                    state = await node.ExecuteAsync(state, ct);
                    sw.Stop();

                    if (_observer != null)
                    {
                        await _observer.OnNodeCompleted(node.Name, state, sw.Elapsed);
                        await _observer.OnStateChanged(oldState, state);
                    }
                }
                catch (Exception ex)
                {
                    sw.Stop();
                    _logger.LogError(ex, "Node '{NodeName}' failed", node.Name);
                    state.Error = $"Node '{node.Name}' failed: {ex.Message}";
                    state.IsComplete = true;

                    if (_observer != null)
                        await _observer.OnNodeFailed(node.Name, state, ex);

                    break;
                }
            }

            graphStopwatch.Stop();
            if (_observer != null)
                await _observer.OnGraphCompleted(state, graphStopwatch.Elapsed);

            _logger.LogInformation("Graph execution completed in {Iterations} iterations", state.Iteration);
            return state;
        }

        private string? GetNextNode(IAgentState state)
        {
            var current = state.CurrentNode;

            // Check conditional routers first
            if (_conditionalRouters.TryGetValue(current, out var router))
            {
                return router(state);
            }

            // Check regular edges
            if (_edges.TryGetValue(current, out var edges))
            {
                // Find first matching edge (with condition or unconditional)
                foreach (var edge in edges)
                {
                    if (edge.Condition == null || edge.Condition(state))
                    {
                        return edge.Target;
                    }
                }
            }

            // No edge found - END
            return GraphConstants.END;
        }

        private record Edge(string Target, Func<IAgentState, bool>? Condition);
    }
}
