using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ControlHub.Application.AI.V3.Agentic.Nodes;
using ControlHub.Application.Common.Interfaces.AI.V3;
using ControlHub.Application.Common.Interfaces.AI.V3.Agentic;
using ControlHub.Application.Common.Interfaces.AI.V3.RAG;
using ControlHub.Application.Common.Interfaces.AI.V3.Reasoning;
using ControlHub.Application.Common.Interfaces.AI.V3.Observability;
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
        private readonly IAgentObserver _observer;
        private readonly ILogger<AuditAgentV3> _logger;

        public AuditAgentV3(
            IStateGraph graph,
            IReasoningModel reasoningModel,
            IAgenticRAG agenticRag,
            IConfidenceScorer confidenceScorer,
            IAgentObserver observer,
            ILogger<AuditAgentV3> logger)
        {
            _graph = graph;
            _reasoningModel = reasoningModel;
            _agenticRag = agenticRag;
            _confidenceScorer = confidenceScorer;
            _observer = observer;
            _logger = logger;

            // Build the graph
            BuildGraph();
        }

        private void BuildGraph()
        {
            // Create nodes
            var plannerLoggerFactory = LoggerFactory.Create(b => b.AddConsole());
            var plannerNode = new PlannerNode(_reasoningModel, _observer, plannerLoggerFactory.CreateLogger<PlannerNode>());
            var executorNode = new ExecutorNode(_agenticRag, _reasoningModel, _observer, plannerLoggerFactory.CreateLogger<ExecutorNode>());
            var verifierNode = new VerifierNode(_confidenceScorer, _observer, plannerLoggerFactory.CreateLogger<VerifierNode>());
            var reflectorNode = new ReflectorNode(_reasoningModel, _observer, plannerLoggerFactory.CreateLogger<ReflectorNode>());

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

            // 1. FINAL DIAGNOSIS (If available from synthesis step)
            var results = state.GetContext<List<string>>("execution_results") ?? new List<string>();
            var synthesis = results.LastOrDefault(r => r.Contains("Root Cause Synthesis", StringComparison.OrdinalIgnoreCase) 
                                                     || r.Contains("ID_", StringComparison.OrdinalIgnoreCase)
                                                     || r.Contains("BatchExecution", StringComparison.OrdinalIgnoreCase));

            if (!string.IsNullOrEmpty(synthesis))
            {
                sb.AppendLine("# 🔍 Final Investigation Diagnosis");
                sb.AppendLine(synthesis);
                sb.AppendLine();
            }

            // 2. DETAILED FINDINGS
            sb.AppendLine("## 📋 Execution Details");
            var plan = state.GetContext<List<string>>("plan") ?? new List<string>();
            for (int i = 0; i < plan.Count; i++)
            {
                var stepResult = results.FirstOrDefault(r => r.StartsWith($"Step {i + 1}:"));
                sb.AppendLine($"### Step {i + 1}: {plan[i]}");
                if (!string.IsNullOrEmpty(stepResult))
                {
                    // Clean up stepResult prefix for cleaner view
                    var cleanerResult = stepResult.Substring(stepResult.IndexOf('\n') + 1);
                    sb.AppendLine(cleanerResult);
                }
                sb.AppendLine();
            }

            // 3. VERIFICATION & REFLEXION
            var passed = state.GetContextValue("verification_passed", false);
            var score = state.GetContextValue("verification_score", 0f);
            sb.AppendLine($"---");
            sb.AppendLine($"**Verification Status:** {(passed ? "✅ PASSED" : "❌ FAILED")} (Confidence: {score:P0})");

            var analysis = state.GetContext<string>("reflexion_analysis");
            if (!string.IsNullOrEmpty(analysis))
            {
                sb.AppendLine();
                sb.AppendLine("> **Agent Reflexion:** " + analysis);
            }

            return sb.ToString();
        }
    }
}
