namespace ControlHub.Application.Common.Interfaces.AI.V3.Agentic
{
    /// <summary>
    /// Agent State interface - LangGraph-inspired state management.
    /// Mỗi node trong graph nhận state và trả về state mới.
    /// </summary>
    public interface IAgentState
    {
        /// <summary>Tên node hiện tại đang xử lý</summary>
        string CurrentNode { get; set; }

        /// <summary>Shared context giữa các nodes</summary>
        Dictionary<string, object> Context { get; }

        /// <summary>Message history (user, agent, tool)</summary>
        List<AgentMessage> Messages { get; }

        /// <summary>Workflow đã hoàn thành chưa</summary>
        bool IsComplete { get; set; }

        /// <summary>Số lần iteration (cho reflexion)</summary>
        int Iteration { get; set; }

        /// <summary>Max iterations allowed</summary>
        int MaxIterations { get; }

        /// <summary>Error nếu có</summary>
        string? Error { get; set; }

        /// <summary>Clone state để tránh mutation</summary>
        IAgentState Clone();
    }

    /// <summary>
    /// Message trong conversation.
    /// </summary>
    public record AgentMessage(
        /// <summary>Role: user, assistant, tool, system</summary>
        string Role,

        /// <summary>Nội dung message</summary>
        string Content,

        /// <summary>Tool name nếu role=tool</summary>
        string? ToolName = null,

        /// <summary>Metadata bổ sung</summary>
        Dictionary<string, string>? Metadata = null
    );
}
