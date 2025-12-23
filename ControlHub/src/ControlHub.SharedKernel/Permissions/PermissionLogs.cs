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

            public static readonly LogCode SearchPermissions_Started =
        new("Permission.Search.Started", "Starting permission search process");

        public static readonly LogCode SearchPermissions_Success =
            new("Permission.Search.Success", "Permission search completed successfully");
    }
}