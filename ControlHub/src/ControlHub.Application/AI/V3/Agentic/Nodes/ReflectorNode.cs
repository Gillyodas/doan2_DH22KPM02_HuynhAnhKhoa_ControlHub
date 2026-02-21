using ControlHub.Application.Common.Interfaces.AI.V3.Agentic;
using ControlHub.Application.Common.Interfaces.AI.V3.Observability;
using ControlHub.Application.Common.Interfaces.AI.V3.Reasoning;
using Microsoft.Extensions.Logging;

namespace ControlHub.Application.AI.V3.Agentic.Nodes
{
    /// <summary>
    /// ReflectorNode - Analyzes failures and suggests corrections.
    /// Implements Reflexion pattern for self-improving agents.
    /// </summary>
    public class ReflectorNode : IAgentNode, IReflexionLoop
    {
        private readonly IReasoningModel _reasoningModel;
        private readonly IAgentObserver? _observer;
        private readonly ILogger<ReflectorNode> _logger;

        public string Name => "Reflector";
        public string Description => "Analyzes failures and suggests corrections";

        public ReflectorNode(IReasoningModel reasoningModel, IAgentObserver? observer, ILogger<ReflectorNode> logger)
        {
            _reasoningModel = reasoningModel;
            _observer = observer;
            _logger = logger;
        }

        public async Task<IAgentState> ExecuteAsync(IAgentState state, CancellationToken ct = default)
        {
            var result = await ReflectAsync(state, ct);
            var clone = (AgentState)state.Clone();

            clone.Context["reflexion_analysis"] = result.Analysis;
            clone.Context["reflexion_corrections"] = result.Corrections;
            clone.Context["reflexion_should_retry"] = result.ShouldRetry;

            clone.Messages.Add(new AgentMessage(
                "assistant",
                $"Reflexion: {result.Analysis}\nCorrections: {result.Corrections}"
            ));

            if (!result.ShouldRetry)
            {
                clone.IsComplete = true;
            }

            return clone;
        }

        public async Task<ReflexionResult> ReflectAsync(IAgentState state, CancellationToken ct = default)
        {
            var clone = state as AgentState;
            if (clone == null) return new ReflexionResult("Unable to cast state", "", false, 0);

            // Gather context for reflection
            var verificationPassed = clone.GetContextValue("verification_passed", false);
            var verificationReason = clone.GetContext<string>("verification_reason") ?? "";
            var executionResults = clone.GetContext<List<string>>("execution_results") ?? new List<string>();
            var query = clone.GetContext<string>("query") ?? "";

            // If verification passed, no need to reflect
            if (verificationPassed)
            {
                _logger.LogInformation("Verification passed, no reflexion needed");
                return new ReflexionResult(
                    Analysis: "Execution successful, no corrections needed.",
                    Corrections: "",
                    ShouldRetry: false,
                    Confidence: 0.95f
                );
            }

            _logger.LogInformation("Reflecting on failed verification: {Reason}", verificationReason);

            // Use LLM to analyze and suggest corrections
            var reflectionPrompt = $@"
Analyze why the following task failed and suggest corrections:

Task: {query}

Execution Results:
{string.Join("\n", executionResults)}

Failure Reason: {verificationReason}

Provide:
1. What went wrong
2. How to fix it
3. Whether it's worth retrying (considering iteration {state.Iteration}/{state.MaxIterations})
";

            var context = new ReasoningContext(
                Query: reflectionPrompt,
                RetrievedDocs: new List<Common.Interfaces.AI.V3.RAG.RankedDocument>()
            );

            var result = await _reasoningModel.ReasonAsync(context, new ReasoningOptions(Temperature: 0.5f), ct);

            // Decide if we should retry based on confidence and iterations remaining
            var shouldRetry = result.Confidence > 0.5f && state.Iteration < state.MaxIterations - 1;

            return new ReflexionResult(
                Analysis: result.Explanation,
                Corrections: result.Solution,
                ShouldRetry: shouldRetry,
                Confidence: result.Confidence
            );
        }
    }
}
