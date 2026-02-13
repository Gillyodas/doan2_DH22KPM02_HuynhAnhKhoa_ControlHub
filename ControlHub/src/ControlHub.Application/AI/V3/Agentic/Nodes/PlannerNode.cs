using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using ControlHub.Application.Common.Interfaces.AI.V3.Agentic;
using ControlHub.Application.Common.Interfaces.AI.V3.Reasoning;
using ControlHub.Application.Common.Interfaces.AI.V3.Observability;
using Microsoft.Extensions.Logging;

namespace ControlHub.Application.AI.V3.Agentic.Nodes
{
    /// <summary>
    /// PlannerNode - Decompose task into actionable steps.
    /// Uses LLM to analyze task and create execution plan.
    /// </summary>
    public class PlannerNode : IAgentNode
    {
        private readonly IReasoningModel _reasoningModel;
        private readonly IAgentObserver? _observer;
        private readonly ILogger<PlannerNode> _logger;

        public string Name => "Planner";
        public string Description => "Analyzes task and creates execution plan";

        public PlannerNode(IReasoningModel reasoningModel, IAgentObserver? observer, ILogger<PlannerNode> logger)
        {
            _reasoningModel = reasoningModel;
            _observer = observer;
            _logger = logger;
        }

        public async Task<IAgentState> ExecuteAsync(IAgentState state, CancellationToken ct = default)
        {
            var clone = (AgentState)state.Clone();
            
            // Get user query from context or last message
            var query = clone.GetContext<string>("query") 
                        ?? clone.Messages.FindLast(m => m.Role == "user")?.Content
                        ?? "";

            if (string.IsNullOrEmpty(query))
            {
                clone.Error = "No query found in state";
                clone.IsComplete = true;
                return clone;
            }

            _logger.LogInformation("Planning for query: {Query}", query);

            // Get correlationId if available to provide more context to the planner
            var correlationId = clone.GetContext<string>("correlationId");
            var enhancedQuery = !string.IsNullOrEmpty(correlationId) 
                ? $"{query} (CorrelationId: {correlationId})" 
                : query;

            // Get pre-retrieved docs if available
            var preRetrievedDocs = clone.GetContext<List<Common.Interfaces.AI.V3.RAG.RankedDocument>>("pre_retrieval_docs") 
                                  ?? new List<Common.Interfaces.AI.V3.RAG.RankedDocument>();

            // Use reasoning model to create plan
            var context = new ReasoningContext(
                Query: $"Create a detailed technical investigation plan for: {enhancedQuery}. " +
                       "IMPORTANT: The plan MUST conclude with a final step named 'Root Cause Synthesis and Recommendations' " +
                       "that aggregates all findings into a developer-friendly report.",
                RetrievedDocs: preRetrievedDocs
            );

            var result = await _reasoningModel.ReasonAsync(context, new ReasoningOptions(EnableCoT: true), ct);

            // Store plan in context
            clone.Context["plan"] = result.Steps;
            clone.Context["plan_explanation"] = result.Explanation;
            clone.Context["current_step"] = 0;

            clone.Messages.Add(new AgentMessage(
                "assistant",
                $"Plan created with {result.Steps.Count} steps: {result.Solution}"
            ));

            _logger.LogInformation("Plan created with {StepCount} steps", result.Steps.Count);

            return clone;
        }
    }
}
