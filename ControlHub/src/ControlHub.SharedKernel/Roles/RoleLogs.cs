using ControlHub.SharedKernel.Common.Logs;

namespace ControlHub.SharedKernel.Roles
{
    public static class RoleLogs
    {
        public static readonly LogCode CreateRoles_Started =
            new("Role.CreateRoles.Started", "Starting role creation process");

        public static readonly LogCode CreateRoles_DuplicateNames =
            new("Role.CreateRoles.DuplicateNames", "Duplicate role names detected");

        public static readonly LogCode CreateRoles_NoValidRole =
            new("Role.CreateRoles.NoValidRole", "No valid roles found after filtering duplicates");

        public static readonly LogCode CreateRoles_MissingPermissions =
            new("Role.CreateRoles.MissingPermissions", "Role missing required permission IDs");

        public static readonly LogCode CreateRoles_NoValidPermissionFound =
            new("Role.CreateRoles.NoValidPermissionFound", "No valid permissions found for provided IDs");

        public static readonly LogCode CreateRoles_RolePrepared =
            new("Role.CreateRoles.RolePrepared", "Role successfully composed and ready for persistence");

        public static readonly LogCode CreateRoles_RolePrepareFailed =
            new("Role.CreateRoles.RolePrepareFailed", "Failed to prepare role entity due to validation errors");

        public static readonly LogCode CreateRoles_NoPersist =
            new("Role.CreateRoles.NoPersist", "No valid roles were persisted to database");

        public static readonly LogCode CreateRoles_Success =
            new("Role.CreateRoles.Success", "Roles created and persisted successfully");

        public static readonly LogCode CreateRoles_Failed =
            new("Role.CreateRoles.Failed", "Unexpected error occurred while creating roles");
    }
}