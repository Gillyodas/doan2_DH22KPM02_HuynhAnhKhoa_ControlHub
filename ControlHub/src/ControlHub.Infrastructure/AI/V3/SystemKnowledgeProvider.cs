using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using ControlHub.Application.Common.Interfaces.AI.V3;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace ControlHub.Infrastructure.AI.V3
{
    /// <summary>
    /// Provides system-level domain knowledge from system_knowledge.json config.
    /// </summary>
    public class SystemKnowledgeProvider : ISystemKnowledgeProvider
    {
        private readonly IConfiguration _config;
        private readonly ILogger<SystemKnowledgeProvider> _logger;

        public SystemKnowledgeProvider(
            IConfiguration config,
            ILogger<SystemKnowledgeProvider> logger)
        {
            _config = config;
            _logger = logger;
        }

        public string GetKnowledgeForErrorCode(string errorCode)
        {
            if (string.IsNullOrEmpty(errorCode))
                return string.Empty;

            // Extract module name from error code (e.g., "Permission" from "Permission.InvalidFormat")
            var parts = errorCode.Split('.');
            if (parts.Length < 2)
                return string.Empty;

            var moduleName = parts[0];
            var moduleSection = _config.GetSection($"SystemKnowledge:modules:{moduleName}");

            if (!moduleSection.Exists())
            {
                _logger.LogDebug("No system knowledge found for module: {Module}", moduleName);
                return string.Empty;
            }

            var sb = new StringBuilder();
            sb.AppendLine($"## System Knowledge — Module: {moduleName}");

            // Description
            var description = moduleSection["description"];
            if (!string.IsNullOrEmpty(description))
                sb.AppendLine($"- **Description**: {description}");

            // Validation rules
            var rules = moduleSection.GetSection("validation_rules").GetChildren().ToList();
            if (rules.Any())
            {
                sb.AppendLine("- **Validation Rules**:");
                foreach (var rule in rules)
                    sb.AppendLine($"  - {rule.Value}");
            }

            // Error code description
            var errorDesc = moduleSection[$"error_codes:{errorCode}"];
            if (!string.IsNullOrEmpty(errorDesc))
                sb.AppendLine($"- **Error `{errorCode}`**: {errorDesc}");

            // Endpoints
            var endpoints = moduleSection.GetSection("endpoints").GetChildren().ToList();
            if (endpoints.Any())
            {
                sb.AppendLine("- **Endpoints**:");
                foreach (var ep in endpoints)
                    sb.AppendLine($"  - {ep.Value}");
            }

            var result = sb.ToString();
            _logger.LogInformation("Found system knowledge for {ErrorCode}: {Length} chars", errorCode, result.Length);
            return result;
        }

        public string GetFullSystemContext()
        {
            var archSection = _config.GetSection("SystemKnowledge:architecture");
            if (!archSection.Exists())
                return string.Empty;

            var sb = new StringBuilder();
            sb.AppendLine("## System Architecture:");

            var description = archSection["description"];
            if (!string.IsNullOrEmpty(description))
                sb.AppendLine($"- {description}");

            var database = archSection["database"];
            if (!string.IsNullOrEmpty(database))
                sb.AppendLine($"- Database: {database}");

            var auth = archSection["auth"];
            if (!string.IsNullOrEmpty(auth))
                sb.AppendLine($"- Auth: {auth}");

            // Infrastructure notes
            var notes = _config.GetSection("SystemKnowledge:infrastructure_notes").GetChildren().ToList();
            if (notes.Any())
            {
                sb.AppendLine("- **Notes**:");
                foreach (var note in notes)
                    sb.AppendLine($"  - {note.Value}");
            }

            return sb.ToString();
        }
    }
}
