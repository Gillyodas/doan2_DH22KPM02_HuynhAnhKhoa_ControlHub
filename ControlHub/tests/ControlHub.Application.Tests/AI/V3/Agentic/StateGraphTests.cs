using ControlHub.Application.AI.V3.Agentic;
using ControlHub.Application.Common.Interfaces.AI.V3.Agentic;
using Microsoft.Extensions.Logging;
using Moq;

namespace ControlHub.Application.Tests.AI.V3.Agentic
{
    public class StateGraphTests
    {
        private readonly Mock<ILogger<StateGraph>> _loggerMock = new();
        private readonly StateGraph _graph;

        public StateGraphTests()
        {
            _graph = new StateGraph(_loggerMock.Object);
        }

        [Fact]
        public async Task RunAsync_WithSimpleGraph_ShouldExecuteNodes()
        {
            // Arrange
            var node1 = new TestNode("Node1");
            var node2 = new TestNode("Node2");
            var node3 = new TestNode("Node3");

            _graph.AddNode(node1);
            _graph.AddNode(node2);
            _graph.AddNode(node3);

            _graph.AddEdge(GraphConstants.START, "Node1");
            _graph.AddEdge("Node1", "Node2");
            _graph.AddEdge("Node2", "Node3");
            _graph.AddEdge("Node3", GraphConstants.END);

            var initialState = new AgentState();

            // Act
            var finalState = await _graph.RunAsync(initialState);

            // Assert
            Assert.True(finalState.IsComplete);
            Assert.Equal(3, finalState.Iteration);
            Assert.Null(finalState.Error);
        }

        [Fact]
        public async Task RunAsync_WithConditionalEdge_ShouldFollowCondition()
        {
            // Arrange
            var node1 = new TestNode("Node1");
            var node2 = new TestNode("Node2");
            var node3 = new TestNode("Node3");

            _graph.AddNode(node1);
            _graph.AddNode(node2);
            _graph.AddNode(node3);

            _graph.AddEdge(GraphConstants.START, "Node1");
            _graph.AddConditionalEdges("Node1", state =>
            {
                var s = state as AgentState;
                return s?.GetContextValue("go_to_3", false) == true ? "Node3" : "Node2";
            });
            _graph.AddEdge("Node2", GraphConstants.END);
            _graph.AddEdge("Node3", GraphConstants.END);

            var state1 = new AgentState();
            state1.Context["go_to_3"] = false;

            var state2 = new AgentState();
            state2.Context["go_to_3"] = true;

            // Act
            var result1 = await _graph.RunAsync(state1);
            var result2 = await _graph.RunAsync(state2);

            // Assert
            Assert.Equal(2, result1.Iteration); // Node1 -> Node2
            Assert.Equal(2, result2.Iteration); // Node1 -> Node3
        }

        [Fact]
        public async Task RunAsync_WithMaxIterations_ShouldStop()
        {
            // Arrange
            var node1 = new TestNode("LoopNode");
            _graph.AddNode(node1);
            _graph.AddEdge(GraphConstants.START, "LoopNode");
            _graph.AddEdge("LoopNode", "LoopNode"); // Infinite loop

            var state = new AgentState(maxIterations: 3);

            // Act
            var result = await _graph.RunAsync(state);

            // Assert
            Assert.True(result.IsComplete);
            Assert.Equal("Max iterations reached", result.Error);
        }

        private class TestNode : IAgentNode
        {
            public string Name { get; }
            public string Description => "Test node";

            public TestNode(string name) => Name = name;

            public Task<IAgentState> ExecuteAsync(IAgentState state, CancellationToken ct = default)
            {
                var clone = (AgentState)state.Clone();
                clone.Messages.Add(new AgentMessage("assistant", $"Executed {Name}"));
                return Task.FromResult<IAgentState>(clone);
            }
        }
    }
}
