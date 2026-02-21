namespace ControlHub.Application.Common.Interfaces.AI.V3.Agentic
{
    /// <summary>
    /// Tool interface - A tool that can be called by the agent.
    /// </summary>
    public interface ITool
    {
        /// <summary>Unique tool name</summary>
        string Name { get; }

        /// <summary>Tool description for LLM</summary>
        string Description { get; }

        /// <summary>Parameters description (JSON Schema)</summary>
        string ParametersSchema { get; }

        /// <summary>Execute the tool</summary>
        System.Threading.Tasks.Task<ToolResult> ExecuteAsync(
            Dictionary<string, object> parameters,
            System.Threading.CancellationToken ct = default
        );
    }

    /// <summary>
    /// Tool execution result.
    /// </summary>
    public record ToolResult(
        /// <summary>Tool output</summary>
        string Output,

        /// <summary>Whether execution succeeded</summary>
        bool Success,

        /// <summary>Error message if failed</summary>
        string? Error = null
    );

    /// <summary>
    /// Tool Registry interface - Manages available tools.
    /// </summary>
    public interface IToolRegistry
    {
        /// <summary>Register a new tool</summary>
        void RegisterTool(ITool tool);

        /// <summary>Get tool by name</summary>
        ITool? GetTool(string name);

        /// <summary>Get all registered tools</summary>
        IEnumerable<ITool> GetAllTools();

        /// <summary>Check if tool exists</summary>
        bool HasTool(string name);

        /// <summary>Get tool descriptions for LLM context</summary>
        string GetToolsDescription();
    }
}
