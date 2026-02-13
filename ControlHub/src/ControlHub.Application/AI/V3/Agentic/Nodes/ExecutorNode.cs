using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ControlHub.Application.Common.Interfaces.AI.V3.Agentic;
using ControlHub.Application.Common.Interfaces.AI.V3.RAG;
using ControlHub.Application.Common.Interfaces.AI.V3.Reasoning;
using ControlHub.Application.Common.Interfaces.AI.V3.Observability;
using Microsoft.Extensions.Logging;

namespace ControlHub.Application.AI.V3.Agentic.Nodes
{
    /// <summary>
    /// ExecutorNode - Execute plan steps using tools and RAG.
    /// </summary>
    public class ExecutorNode : IAgentNode
    {
        private readonly IAgenticRAG _agenticRag;
        private readonly IReasoningModel _reasoningModel;
        private readonly IAgentObserver? _observer;
        private readonly ILogger<ExecutorNode> _logger;

        public string Name => "Executor";
        public string Description => "Executes plan steps using available tools";

        public ExecutorNode(
            IAgenticRAG agenticRag, 
            IReasoningModel reasoningModel,
            IAgentObserver? observer, 
            ILogger<ExecutorNode> logger)
        {
            _agenticRag = agenticRag;
            _reasoningModel = reasoningModel;
            _observer = observer;
            _logger = logger;
        }

        public async Task<IAgentState> ExecuteAsync(IAgentState state, CancellationToken ct = default)
        {
            var clone = (AgentState)state.Clone();

            // Get plan and current step
            var plan = clone.GetContext<List<string>>("plan");
            var currentStep = clone.GetContextValue("current_step", 0);

            if (plan == null || !plan.Any())
            {
                clone.Error = "No plan found. Planner node must run first.";
                clone.IsComplete = true;
                return clone;
            }

            // Phase 4 & 5: Batch Execution - Nếu currentStep > 0 nghĩa là đã thực hiện batch rồi
            if (currentStep > 0)
            {
                _logger.LogInformation("Batch execution already completed. Skipping Executor loop.");
                clone.Context["execution_complete"] = true;
                return clone;
            }

            _logger.LogInformation("Starting BATCH execution for {Count} steps", plan.Count);

            // Get correlationId from state (if provided)
            var correlationId = clone.GetContext<string>("correlationId");
            var originalQuery = clone.GetContext<string>("query") ?? "IT Investigation";

            // Step 1: Use pre-retrieved docs or call RAG once with the combined query
            var preRetrievedDocs = clone.GetContext<List<RankedDocument>>("pre_retrieval_docs");
            List<RankedDocument> evidence;

            if (preRetrievedDocs != null && preRetrievedDocs.Any())
            {
                _logger.LogInformation("Executor: Using {Count} pre-retrieved evidence documents", preRetrievedDocs.Count);
                evidence = preRetrievedDocs;
            }
            else
            {
                _logger.LogInformation("Executor: No pre-retrieval docs found, performing batch retrieval");
                var ragOptions = new AgenticRAGOptions(CorrelationId: correlationId);
                var ragResult = await _agenticRag.RetrieveAsync(originalQuery, ragOptions, ct);
                evidence = ragResult.Documents;
            }

            // Step 2: Build batch execution prompt
            var planText = string.Join("\n", plan.Select((s, i) => $"{i + 1}. {s}"));
            var batchPrompt = 
                $"You are executing a technical investigation plan gộp (batch execution).\n\n" +
                $"## Original Query:\n{originalQuery}\n\n" +
                $"## Investigation Plan:\n{planText}\n\n" +
                $"## Tasks:\n" +
                $"Analyze the provided evidence and provide detailed findings for EACH step of the plan. " +
                $"Format your response as a numbered list of findings corresponding to the plan steps.\n" +
                $"IMPORTANT: Your output for each step MUST start with 'Step X: [Step Name]' followed by the findings.";

            var batchContext = new ReasoningContext(
                Query: batchPrompt,
                RetrievedDocs: evidence
            );

            // Step 3: Single LLM call for the entire plan
            var analysis = await _reasoningModel.ReasonAsync(
                batchContext, 
                new ReasoningOptions(Temperature: 0.2f, MaxTokens: 4096), // Lower temp for factual accuracy
                ct
            );

            // Step 4: Parse and store results
            var executionResults = new List<string>();
            
            // If the LLM returned structured steps in its response, use them. 
            // Otherwise, we'll use its 'solution' text which contains the findings.
            if (analysis.Steps.Any() && analysis.Steps.Count >= plan.Count)
            {
                for (int i = 0; i < plan.Count; i++)
                {
                    var findings = analysis.Steps[i];
                    executionResults.Add($"Step {i + 1}: {plan[i]}\n{findings}\n_(Source: {evidence.Count} logs via BatchExecution)_");
                }
            }
            else
            {
                // Fallback: Use the whole solution text if it couldn't be parsed into steps
                // We'll split by "Step X:" manually if needed, or just use the whole thing for the last step
                _logger.LogWarning("LLM did not return structured steps for batch execution, using fallback parsing");
                executionResults.Add($"Batch Findings for all {plan.Count} steps:\n{analysis.Solution}\n_(Source: {evidence.Count} logs via BatchExecution)_");
            }

            clone.Context["execution_results"] = executionResults;
            clone.Context["current_step"] = plan.Count; // Mark all steps as complete
            clone.Context["execution_complete"] = true;

            clone.Messages.Add(new AgentMessage(
                "assistant",
                $"Executed all {plan.Count} steps in batch mode: {analysis.Solution.Take(100)}...",
                "Executor"
            ));

            return clone;
        }
    }
}
