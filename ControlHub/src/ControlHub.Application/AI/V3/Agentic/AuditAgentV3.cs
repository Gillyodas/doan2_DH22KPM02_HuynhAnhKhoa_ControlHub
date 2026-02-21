using ControlHub.Application.AI.V3.Agentic.Nodes;
using ControlHub.Application.Common.Interfaces.AI.V3;
using ControlHub.Application.Common.Interfaces.AI.V3.Agentic;
using ControlHub.Application.Common.Interfaces.AI.V3.Observability;
using ControlHub.Application.Common.Interfaces.AI.V3.RAG;
using ControlHub.Application.Common.Interfaces.AI.V3.Reasoning;
using Microsoft.Extensions.Logging;

namespace ControlHub.Application.AI.V3.Agentic
{
    /// <summary>
    /// AuditAgentV3 - Full agentic audit agent with graph orchestration.
    /// Implements Plan-Execute-Verify-Reflect loop.
    /// </summary>
    public class AuditAgentV3 : IAuditAgentV3
    {
        private readonly IStateGraph _graph;
        private readonly IReasoningModel _reasoningModel;
        private readonly IAgenticRAG _agenticRag;
        private readonly IConfidenceScorer _confidenceScorer;
        private readonly ISystemKnowledgeProvider _knowledgeProvider;
        private readonly IAgentObserver _observer;
        private readonly ILoggerFactory _loggerFactory;
        private readonly ILogger<AuditAgentV3> _logger;

        public AuditAgentV3(
            IStateGraph graph,
            IReasoningModel reasoningModel,
            IAgenticRAG agenticRag,
            IConfidenceScorer confidenceScorer,
            ISystemKnowledgeProvider knowledgeProvider,
            IAgentObserver observer,
            ILoggerFactory loggerFactory,
            ILogger<AuditAgentV3> logger)
        {
            _graph = graph;
            _reasoningModel = reasoningModel;
            _agenticRag = agenticRag;
            _confidenceScorer = confidenceScorer;
            _knowledgeProvider = knowledgeProvider;
            _observer = observer;
            _loggerFactory = loggerFactory;
            _logger = logger;

            // Build the graph
            BuildGraph();
        }

        private void BuildGraph()
        {
            // Create nodes using the shared logger factory
            var plannerNode = new PlannerNode(_reasoningModel, _observer, _loggerFactory.CreateLogger<PlannerNode>());
            var executorNode = new ExecutorNode(_agenticRag, _reasoningModel, _knowledgeProvider, _observer, _loggerFactory.CreateLogger<ExecutorNode>());
            var verifierNode = new VerifierNode(_confidenceScorer, _observer, _loggerFactory.CreateLogger<VerifierNode>());
            var reflectorNode = new ReflectorNode(_reasoningModel, _observer, _loggerFactory.CreateLogger<ReflectorNode>());

            // Add nodes
            _graph.AddNode(plannerNode);
            _graph.AddNode(executorNode);
            _graph.AddNode(verifierNode);
            _graph.AddNode(reflectorNode);

            // Define edges: START → Planner → Executor (loop until complete) → Verifier → conditional
            _graph.AddEdge(GraphConstants.START, "Planner");
            _graph.AddEdge("Planner", "Executor");

            // Executor loops until all steps done
            _graph.AddConditionalEdges("Executor", state =>
            {
                // Sau khi Batch Execution xong (current_step = plan.Count), đi thẳng Verifier
                return "Verifier";
            });

            // Verifier → Reflector OR END
            _graph.AddConditionalEdges("Verifier", state =>
            {
                var clone = state as AgentState;
                var passed = clone?.GetContextValue("verification_passed", false) ?? false;

                if (passed)
                    return GraphConstants.END;

                return "Reflector";
            });

            // Reflector → Planner (retry) OR END
            _graph.AddConditionalEdges("Reflector", state =>
            {
                var clone = state as AgentState;
                var shouldRetry = clone?.GetContextValue("reflexion_should_retry", false) ?? false;

                if (shouldRetry)
                {
                    // Reset execution state for retry
                    clone!.Context["current_step"] = 0;
                    clone.Context.Remove("execution_results");
                    return "Planner";
                }

                return GraphConstants.END;
            });
        }

        public IStateGraph GetGraph() => _graph;

        public async Task<AgentExecutionResult> InvestigateAsync(
            string query,
            string? correlationId = null,
            CancellationToken ct = default)
        {
            _logger.LogInformation("AuditAgentV3 starting investigation: {Query}", query);

            var initialState = new AgentState(maxIterations: 50);
            initialState.Context["query"] = query;
            if (!string.IsNullOrEmpty(correlationId))
                initialState.Context["correlationId"] = correlationId;

            initialState.Messages.Add(new AgentMessage("user", query));

            // Phase 3: Pre-Retrieval - Lấy evidence trước khi plan để LLM không bị "mù"
            _logger.LogInformation("AgenticRAG: Performing pre-retrieval for query: {Query}", query);
            var ragOptions = new AgenticRAGOptions(CorrelationId: correlationId);
            var ragResult = await _agenticRag.RetrieveAsync(query, ragOptions, ct);

            initialState.Context["pre_retrieval_docs"] = ragResult.Documents;
            initialState.Context["pre_retrieval_strategy"] = ragResult.StrategyUsed.ToString();
            initialState.Context["rag_metadata"] = ragResult.Metadata;

            // Run graph
            var finalState = (AgentState)await _graph.RunAsync(initialState, ct);

            // Extract results
            var plan = finalState.GetContext<List<string>>("plan") ?? new List<string>();
            var executionResults = finalState.GetContext<List<string>>("execution_results") ?? new List<string>();
            var verificationPassed = finalState.GetContextValue("verification_passed", false);
            var verificationScore = finalState.GetContextValue("verification_score", 0f);

            // Build final answer
            var answer = BuildAnswer(finalState);

            _logger.LogInformation(
                "Investigation complete: {Passed}, {Iterations} iterations",
                verificationPassed ? "PASSED" : "FAILED",
                finalState.Iteration
            );

            return new AgentExecutionResult(
                Answer: answer,
                Plan: plan,
                ExecutionResults: executionResults,
                VerificationPassed: verificationPassed,
                Iterations: finalState.Iteration,
                Confidence: verificationScore,
                Error: finalState.Error
            );
        }

        private string BuildAnswer(AgentState state)
        {
            var sb = new System.Text.StringBuilder();
            var results = state.GetContext<List<string>>("execution_results") ?? new List<string>();

            // 1. MAIN DIAGNOSIS — render execution results directly (they are now structured as Problem/Root Cause/Recommendation)
            if (results.Any())
            {
                sb.AppendLine("# 🔍 Investigation Report");
                sb.AppendLine();
                foreach (var section in results)
                {
                    sb.AppendLine(section);
                    sb.AppendLine();
                }
            }
            else
            {
                sb.AppendLine("# ⚠️ Investigation Incomplete");
                sb.AppendLine("No findings were produced during the investigation.");
                sb.AppendLine();
            }

            // 2. INVESTIGATION PLAN (for reference)
            var plan = state.GetContext<List<string>>("plan") ?? new List<string>();
            if (plan.Any())
            {
                sb.AppendLine("---");
                sb.AppendLine("## 📋 Investigation Plan (" + plan.Count + " steps)");
                sb.AppendLine();
                for (int i = 0; i < plan.Count; i++)
                {
                    sb.AppendLine($"{i + 1}. {plan[i]}");
                }
                sb.AppendLine();
            }

            // 3. VERIFICATION & REFLEXION
            var passed = state.GetContextValue("verification_passed", false);
            var score = state.GetContextValue("verification_score", 0f);
            sb.AppendLine($"**Verification:** {(passed ? "✅ PASSED" : "❌ FAILED")} (Confidence: {score:P0})");

            var analysis = state.GetContext<string>("reflexion_analysis");
            if (!string.IsNullOrEmpty(analysis))
            {
                sb.AppendLine();
                sb.AppendLine("> **Agent Reflexion:** " + analysis);
            }

            // 4. RUNBOOK SUGGESTION — detect new error patterns
            var ragMeta = state.GetContext<Dictionary<string, object>>("rag_metadata");
            if (ragMeta != null && ragMeta.TryGetValue("evidence_metadata", out var eMeta) && eMeta is LogMetadata meta && meta.ErrorCode != null)
            {
                // Check if any runbook was retrieved for this error code
                var docs = state.GetContext<List<RankedDocument>>("pre_retrieval_docs") ?? new List<RankedDocument>();
                var hasRunbook = docs.Any(d =>
                    d.Metadata.GetValueOrDefault("is_runbook") == "true" &&
                    d.Content.Contains(meta.ErrorCode, System.StringComparison.OrdinalIgnoreCase));

                if (!hasRunbook)
                {
                    sb.AppendLine();
                    sb.AppendLine("---");
                    sb.AppendLine($"> 💡 **New pattern detected**: No existing runbook for `{meta.ErrorCode}`. Consider adding a runbook for faster resolution next time.");
                }
            }

            return sb.ToString();
        }
    }
}
