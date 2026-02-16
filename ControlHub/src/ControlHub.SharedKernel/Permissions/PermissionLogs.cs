using ControlHub.SharedKernel.Common.Logs;

namespace ControlHub.SharedKernel.Permissions
{
    public static class PermissionLogs
    {
        public static readonly LogCode CreatePermissions_Started =
            new("Permission.Create.Started", "Starting permission creation process");

        public static readonly LogCode CreatePermissions_Duplicate =
            new("Permission.Create.Duplicate", "Duplicate permission codes found");

        public static readonly LogCode CreatePermissions_Success =
            new("Permission.Create.Success", "Permissions created successfully");

        public static readonly LogCode CreatePermissions_Failed =
            new("Permission.Create.Failed", "Permission creation failed");

        public static readonly LogCode CreatePermissions_DomainError =
            new("Permission.Create.DomainError", "Domain validation failed for permission");

        public static readonly LogCode SearchPermissions_Started =
            new("Permission.Search.Started", "Starting permission search process");

        public static readonly LogCode SearchPermissions_Success =
            new("Permission.Search.Success", "Permission search completed successfully");

        // Update Permission
        public static readonly LogCode UpdatePermission_Started =
            new("Permission.Update.Started", "Starting permission update process");

        public static readonly LogCode UpdatePermission_NotFound =
            new("Permission.Update.NotFound", "Permission not found for update");

        public static readonly LogCode UpdatePermission_Success =
            new("Permission.Update.Success", "Permission updated successfully");

        // Delete Permission
        public static readonly LogCode DeletePermission_Started =
            new("Permission.Delete.Started", "Starting permission deletion process");

        public static readonly LogCode DeletePermission_NotFound =
            new("Permission.Delete.NotFound", "Permission not found for deletion");

        public static readonly LogCode DeletePermission_Success =
            new("Permission.Delete.Success", "Permission deleted successfully");
    }
}
