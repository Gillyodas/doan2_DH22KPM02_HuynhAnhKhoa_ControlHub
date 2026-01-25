namespace ControlHub.Domain.Permissions
{
    /// <summary>
    /// Static permissions for the ControlHub system
    /// </summary>
    public static class Permissions
    {
        // Authentication Permissions
        public const string SignIn = "auth.signin";
        public const string Register = "auth.register";
        public const string RefreshToken = "auth.refresh";
        public const string ChangePassword = "auth.change_password";
        public const string ForgotPassword = "auth.forgot_password";
        public const string ResetPassword = "auth.reset_password";

        // User Management Permissions
        public const string ViewUsers = "users.view";
        public const string CreateUser = "users.create";
        public const string UpdateUser = "users.update";
        public const string DeleteUser = "users.delete";
        public const string UpdateUsername = "users.update_username";

        // Role Management Permissions
        public const string ViewRoles = "roles.view";
        public const string CreateRole = "roles.create";
        public const string UpdateRole = "roles.update";
        public const string DeleteRole = "roles.delete";
        public const string AssignRole = "roles.assign";

        // Identifier Configuration Permissions
        public const string ViewIdentifierConfigs = "identifiers.view";
        public const string CreateIdentifierConfig = "identifiers.create";
        public const string UpdateIdentifierConfig = "identifiers.update";
        public const string DeleteIdentifierConfig = "identifiers.delete";
        public const string ToggleIdentifierConfig = "identifiers.toggle";

        // System Administration Permissions
        public const string ViewSystemLogs = "system.view_logs";
        public const string ViewSystemMetrics = "system.view_metrics";
        public const string ManageSystemSettings = "system.manage_settings";
        public const string ViewAuditLogs = "system.view_audit";

        // Profile Permissions (all users can manage their own profile)
        public const string ViewOwnProfile = "profile.view_own";
        public const string UpdateOwnProfile = "profile.update_own";

        // Permission Management (SuperAdmin only)
        public const string ViewPermissions = "permissions.view";
        public const string CreatePermission = "permissions.create";
        public const string UpdatePermission = "permissions.update";
        public const string DeletePermission = "permissions.delete";
        public const string AssignPermission = "permissions.assign";
    }

    /// <summary>
    /// Permission policies for authorization
    /// </summary>
    public static class Policies
    {
        public const string CanSignIn = "Permission:" + Permissions.SignIn;
        public const string CanRegister = "Permission:" + Permissions.Register;
        public const string CanRefreshToken = "Permission:" + Permissions.RefreshToken;
        public const string CanChangePassword = "Permission:" + Permissions.ChangePassword;
        public const string CanForgotPassword = "Permission:" + Permissions.ForgotPassword;
        public const string CanResetPassword = "Permission:" + Permissions.ResetPassword;

        public const string CanViewUsers = "Permission:" + Permissions.ViewUsers;
        public const string CanCreateUser = "Permission:" + Permissions.CreateUser;
        public const string CanUpdateUser = "Permission:" + Permissions.UpdateUser;
        public const string CanDeleteUser = "Permission:" + Permissions.DeleteUser;
        public const string CanUpdateUsername = "Permission:" + Permissions.UpdateUsername;

        public const string CanViewRoles = "Permission:" + Permissions.ViewRoles;
        public const string CanCreateRole = "Permission:" + Permissions.CreateRole;
        public const string CanUpdateRole = "Permission:" + Permissions.UpdateRole;
        public const string CanDeleteRole = "Permission:" + Permissions.DeleteRole;
        public const string CanAssignRole = "Permission:" + Permissions.AssignRole;

        public const string CanViewIdentifierConfigs = "Permission:" + Permissions.ViewIdentifierConfigs;
        public const string CanCreateIdentifierConfig = "Permission:" + Permissions.CreateIdentifierConfig;
        public const string CanUpdateIdentifierConfig = "Permission:" + Permissions.UpdateIdentifierConfig;
        public const string CanDeleteIdentifierConfig = "Permission:" + Permissions.DeleteIdentifierConfig;
        public const string CanToggleIdentifierConfig = "Permission:" + Permissions.ToggleIdentifierConfig;

        public const string CanViewSystemLogs = "Permission:" + Permissions.ViewSystemLogs;
        public const string CanViewSystemMetrics = "Permission:" + Permissions.ViewSystemMetrics;
        public const string CanManageSystemSettings = "Permission:" + Permissions.ManageSystemSettings;
        public const string CanViewAuditLogs = "Permission:" + Permissions.ViewAuditLogs;

        public const string CanViewOwnProfile = "Permission:" + Permissions.ViewOwnProfile;
        public const string CanUpdateOwnProfile = "Permission:" + Permissions.UpdateOwnProfile;

        public const string CanViewPermissions = "Permission:" + Permissions.ViewPermissions;
        public const string CanCreatePermission = "Permission:" + Permissions.CreatePermission;
        public const string CanUpdatePermission = "Permission:" + Permissions.UpdatePermission;
        public const string CanDeletePermission = "Permission:" + Permissions.DeletePermission;
        public const string CanAssignPermission = "Permission:" + Permissions.AssignPermission;
    }
}
