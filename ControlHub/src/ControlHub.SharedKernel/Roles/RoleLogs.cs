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

        public static readonly LogCode SetPermissions_Started =
        new("Role.SetPermissions.Started", "Starting to set permissions for role");

        public static readonly LogCode SetPermissions_InvalidRoleId =
            new("Role.SetPermissions.InvalidRoleId", "Role ID provided has an invalid format");

        public static readonly LogCode SetPermissions_RoleNotFound =
            new("Role.SetPermissions.RoleNotFound", "Role was not found in the database");

        public static readonly LogCode SetPermissions_Finished =
            new("Role.SetPermissions.Finished", "Finished setting permissions for role");

        public static readonly LogCode SearchRoles_Started =
    new("Role.SearchRoles.Started", "Starting role search process");

        public static readonly LogCode SearchRoles_Success =
            new("Role.SearchRoles.Success", "Role search completed successfully");

        public static readonly LogCode AssignRole_Started =
            new("Role.AssignRole.Started", "Starting assign role to user process");

        public static readonly LogCode AssignRole_UserNotFound =
            new("Role.AssignRole.UserNotFound", "User not found for role assignment");

        public static readonly LogCode AssignRole_AccountNotFound =
            new("Role.AssignRole.AccountNotFound", "Account not found for role assignment");

        public static readonly LogCode AssignRole_RoleNotFound =
            new("Role.AssignRole.RoleNotFound", "Target role not found for assignment");

        public static readonly LogCode AssignRole_Success =
            new("Role.AssignRole.Success", "Role assigned to user successfully");

        public static readonly LogCode GetUserRoles_Started =
            new("Role.GetUserRoles.Started", "Fetching roles for user");

        public static readonly LogCode GetUserRoles_Success =
            new("Role.GetUserRoles.Success", "User roles retrieved successfully");

        // Update Role Logs
        public static readonly LogCode UpdateRole_Started =
            new("Role.UpdateRole.Started", "Starting role update process");

        public static readonly LogCode UpdateRole_NotFound =
            new("Role.UpdateRole.NotFound", "Role to update not found");

        public static readonly LogCode UpdateRole_Confict =
            new("Role.UpdateRole.Conflict", "Role name conflict during update");

        public static readonly LogCode UpdateRole_Success =
            new("Role.UpdateRole.Success", "Role updated successfully");

        // Delete Role Logs
        public static readonly LogCode DeleteRole_Started =
            new("Role.DeleteRole.Started", "Starting role deletion process");

        public static readonly LogCode DeleteRole_NotFound =
            new("Role.DeleteRole.NotFound", "Role to delete not found");

        public static readonly LogCode DeleteRole_InUse =
            new("Role.DeleteRole.InUse", "Cannot delete role because it is assigned to users");

        public static readonly LogCode DeleteRole_Success =
            new("Role.DeleteRole.Success", "Role deleted successfully");

        // Get Role Permissions
        public static readonly LogCode GetRolePermissions_Started =
            new("Role.GetRolePermissions.Started", "Fetching permissions for role");

        public static readonly LogCode GetRolePermissions_Success =
            new("Role.GetRolePermissions.Success", "Role permissions retrieved successfully");
    }
}
