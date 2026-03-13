using ControlHub.Application.AuditAI.Interfaces.V3.Agentic;
using ControlHub.Application.AuditAI.Interfaces.V3.Observability;
using ControlHub.Application.AuditAI.Interfaces.V3.Reasoning;
using Microsoft.Extensions.Logging;

namespace ControlHub.Infrastructure.AI.V3.Agentic.Nodes
{
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

            var correlationId = clone.GetContext<string>("correlationId");
            var enhancedQuery = !string.IsNullOrEmpty(correlationId)
                ? $"{query} (CorrelationId: {correlationId})"
                : query;

            var preRetrievedDocs = clone.GetContext<List<Application.AuditAI.Interfaces.V3.RAG.RankedDocument>>("pre_retrieval_docs")
                                  ?? new List<Application.AuditAI.Interfaces.V3.RAG.RankedDocument>();

            var docsContext = preRetrievedDocs.Any()
                ? "## Pre-Retrieved Evidence (use to inform your hypotheses):\n" +
                  string.Join("\n", preRetrievedDocs.Take(5).Select((d, i) =>
                      $"[{i + 1}] {d.Content[..Math.Min(250, d.Content.Length)]}"))
                : "";

            var context = new ReasoningContext(
                Query: $"You are a senior incident investigator.\n\n" +
                       $"{docsContext}\n\n" +
                       $"Create a hypothesis-driven investigation plan for: {enhancedQuery}\n\n" +
                       $"Each step in your plan (in 'steps') must be a falsifiable hypothesis:\n" +
                       $"\"Hypothesis: [suspected cause] — Check: [specific log field/pattern to examine] — Confirms if: [expected finding]\"\n\n" +
                       $"Your 'solution' should be a one-line summary of the investigation approach.\n" +
                       $"Your 'explanation' should state WHY this plan will identify the root cause.\n" +
                       $"Your 'steps' must be the ordered list of investigation hypotheses (maximum 6).\n" +
                       $"The LAST step must always be: \"Root Cause Synthesis — Chain all confirmed hypotheses " +
                       $"into a single causal narrative and produce developer-friendly recommendations.\"",
                RetrievedDocs: preRetrievedDocs
            );

            var result = await _reasoningModel.ReasonAsync(context, new ReasoningOptions(EnableCoT: true), ct);

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
