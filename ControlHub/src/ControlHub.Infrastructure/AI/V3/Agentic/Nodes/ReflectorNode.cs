using ControlHub.Application.AuditAI.Interfaces.V3.Agentic;
using ControlHub.Application.AuditAI.Interfaces.V3.Observability;
using ControlHub.Application.AuditAI.Interfaces.V3.Reasoning;
using Microsoft.Extensions.Logging;

namespace ControlHub.Infrastructure.AI.V3.Agentic.Nodes
{
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
                clone.IsComplete = true;

            return clone;
        }

        public async Task<ReflexionResult> ReflectAsync(IAgentState state, CancellationToken ct = default)
        {
            var clone = state as AgentState;
            if (clone == null) return new ReflexionResult("Unable to cast state", "", false, 0);

            var verificationPassed = clone.GetContextValue("verification_passed", false);
            var verificationReason = clone.GetContext<string>("verification_reason") ?? "";
            var executionResults = clone.GetContext<List<string>>("execution_results") ?? new List<string>();
            var query = clone.GetContext<string>("query") ?? "";

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

            var reflectionPrompt =
                $"You are reviewing a failed AI investigation and deciding whether to retry.\n\n" +
                $"Task: {query}\n" +
                $"Iteration: {state.Iteration} of {state.MaxIterations}\n" +
                $"Failure Reason: {verificationReason}\n\n" +
                $"Execution Results Produced:\n{string.Join("\n---\n", executionResults)}\n\n" +
                $"Answer these questions:\n" +
                $"1. WHAT WENT WRONG: What specific gap in the analysis caused the failure?\n" +
                $"2. CORRECTION: What different investigation approach should be taken on retry?\n" +
                $"3. RETRY_CONFIDENCE (0.0–1.0): How likely is a retry to succeed given the available evidence?\n" +
                $"   Output this as a float on its own line, prefixed exactly with 'RETRY_CONFIDENCE:'\n\n" +
                $"Note: A retry will only occur if RETRY_CONFIDENCE > 0.5 and iterations remain.";

            var context = new ReasoningContext(
                Query: reflectionPrompt,
                RetrievedDocs: new List<Application.AuditAI.Interfaces.V3.RAG.RankedDocument>()
            );

            var result = await _reasoningModel.ReasonAsync(context, new ReasoningOptions(Temperature: 0.5f), ct);

            // Parse explicit RETRY_CONFIDENCE token from raw response; fall back to model's confidence score
            bool shouldRetry;
            var raw = result.RawResponse ?? result.Solution ?? "";
            var confidenceLine = raw.Split('\n')
                .FirstOrDefault(l => l.TrimStart().StartsWith("RETRY_CONFIDENCE:", StringComparison.OrdinalIgnoreCase));
            if (confidenceLine != null &&
                float.TryParse(
                    confidenceLine.Split(':', 2)[1].Trim(),
                    System.Globalization.NumberStyles.Float,
                    System.Globalization.CultureInfo.InvariantCulture,
                    out var parsedConf))
            {
                shouldRetry = parsedConf > 0.5f && state.Iteration < state.MaxIterations - 1;
            }
            else
            {
                shouldRetry = result.Confidence > 0.5f && state.Iteration < state.MaxIterations - 1;
            }

            return new ReflexionResult(
                Analysis: result.Explanation,
                Corrections: result.Solution,
                ShouldRetry: shouldRetry,
                Confidence: result.Confidence
            );
        }
    }
}
