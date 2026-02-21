using System.Text;
using ControlHub.Application.Common.Interfaces.AI.V3.Agentic;
using Microsoft.Extensions.Logging;

namespace ControlHub.Application.AI.V3.Agentic
{
    /// <summary>
    /// Tool Registry - Manages available tools for agent use.
    /// </summary>
    public class ToolRegistry : IToolRegistry
    {
        private readonly Dictionary<string, ITool> _tools = new();
        private readonly ILogger<ToolRegistry> _logger;

        public ToolRegistry(ILogger<ToolRegistry> logger)
        {
            _logger = logger;
        }

        public void RegisterTool(ITool tool)
        {
            _tools[tool.Name] = tool;
            _logger.LogInformation("Registered tool: {ToolName}", tool.Name);
        }

        public ITool? GetTool(string name)
        {
            return _tools.TryGetValue(name, out var tool) ? tool : null;
        }

        public IEnumerable<ITool> GetAllTools() => _tools.Values;

        public bool HasTool(string name) => _tools.ContainsKey(name);

        public string GetToolsDescription()
        {
            var sb = new StringBuilder();
            sb.AppendLine("Available tools:");
            foreach (var tool in _tools.Values)
            {
                sb.AppendLine($"- {tool.Name}: {tool.Description}");
            }
            return sb.ToString();
        }
    }
}
