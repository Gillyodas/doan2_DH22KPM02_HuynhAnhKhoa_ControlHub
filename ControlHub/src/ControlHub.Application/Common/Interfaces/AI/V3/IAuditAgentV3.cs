using ControlHub.Application.Common.Interfaces.AI.V3.Agentic;

namespace ControlHub.Application.Common.Interfaces.AI.V3
{
    /// <summary>
    /// Audit Agent V3 interface - Full agentic audit with graph orchestration.
    /// </summary>
    public interface IAuditAgentV3
    {
        /// <summary>
        /// Investigate an audit task using agentic workflow.
        /// </summary>
        /// <param name="query">User query/task</param>
        /// <param name="correlationId">Optional correlation ID for log retrieval</param>
        /// <param name="ct">Cancellation token</param>
        /// <returns>Agent execution result</returns>
        Task<AgentExecutionResult> InvestigateAsync(
            string query,
            string? correlationId = null,
            CancellationToken ct = default
        );

        /// <summary>
        /// Get the current state graph for debugging.
        /// </summary>
        IStateGraph GetGraph();
    }

    /// <summary>
    /// Result from agent execution.
    /// </summary>
    public record AgentExecutionResult(
        /// <summary>Final answer/solution</summary>
        string Answer,

        /// <summary>Execution plan that was followed</summary>
        System.Collections.Generic.List<string> Plan,

        /// <summary>Results from each execution step</summary>
        System.Collections.Generic.List<string> ExecutionResults,

        /// <summary>Whether verification passed</summary>
        bool VerificationPassed,

        /// <summary>Number of iterations (reflexion loops)</summary>
        int Iterations,

        /// <summary>Confidence score</summary>
        float Confidence,

        /// <summary>Any error message</summary>
        string? Error = null
    );
}
