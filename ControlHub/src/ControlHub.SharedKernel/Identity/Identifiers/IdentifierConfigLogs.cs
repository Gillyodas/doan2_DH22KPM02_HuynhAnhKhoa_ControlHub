using ControlHub.SharedKernel.Common.Logs;

namespace ControlHub.SharedKernel.Identity.Identifiers
{
    public static class IdentifierConfigLogs
    {
        // Create
        public static readonly LogCode CreateConfig_Started =
            new("IdentifierConfig.Create.Started", "Starting identifier configuration creation");

        public static readonly LogCode CreateConfig_Duplicate =
            new("IdentifierConfig.Create.DuplicateName", "Identifier configuration name already exists");

        public static readonly LogCode CreateConfig_RuleFailed =
            new("IdentifierConfig.Create.RuleFailed", "Failed to add validation rule to configuration");

        public static readonly LogCode CreateConfig_PersistFailed =
            new("IdentifierConfig.Create.PersistFailed", "Failed to persist identifier configuration");

        public static readonly LogCode CreateConfig_Success =
            new("IdentifierConfig.Create.Success", "Identifier configuration created successfully");

        // Update
        public static readonly LogCode UpdateConfig_Started =
            new("IdentifierConfig.Update.Started", "Starting identifier configuration update");

        public static readonly LogCode UpdateConfig_NotFound =
            new("IdentifierConfig.Update.NotFound", "Identifier configuration not found");

        public static readonly LogCode UpdateConfig_DuplicateName =
            new("IdentifierConfig.Update.DuplicateName", "New identifier configuration name already exists");

        public static readonly LogCode UpdateConfig_Success =
            new("IdentifierConfig.Update.Success", "Identifier configuration updated successfully");

        // Toggle Active
        public static readonly LogCode ToggleActive_Started =
            new("IdentifierConfig.ToggleActive.Started", "Starting toggle active status");

        public static readonly LogCode ToggleActive_NotFound =
            new("IdentifierConfig.ToggleActive.NotFound", "Identifier configuration not found");

        public static readonly LogCode ToggleActive_Success =
            new("IdentifierConfig.ToggleActive.Success", "Identifier configuration status toggled successfully");

        // Get/Search
        public static readonly LogCode GetConfigs_Started =
            new("IdentifierConfig.Get.Started", "Starting request to retrieve identifier configurations");

        public static readonly LogCode GetConfigs_Failed =
            new("IdentifierConfig.Get.Failed", "Failed to retrieve identifier configurations");

        public static readonly LogCode GetConfigs_Success =
            new("IdentifierConfig.Get.Success", "Identifier configurations retrieved successfully");
    }
}
