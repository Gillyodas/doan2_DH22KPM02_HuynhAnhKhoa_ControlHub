namespace ControlHub.Application.Common.Interfaces.AI.V3.Agentic
{
    /// <summary>
    /// Agent Node interface - Một node trong state graph.
    /// Mỗi node nhận state, xử lý, và trả về state mới.
    /// </summary>
    public interface IAgentNode
    {
        /// <summary>Tên unique của node</summary>
        string Name { get; }

        /// <summary>Description cho debugging/observability</summary>
        string Description { get; }

        /// <summary>
        /// Execute node logic.
        /// </summary>
        /// <param name="state">Current agent state</param>
        /// <param name="ct">Cancellation token</param>
        /// <returns>Updated agent state</returns>
        Task<IAgentState> ExecuteAsync(IAgentState state, CancellationToken ct = default);
    }
}
