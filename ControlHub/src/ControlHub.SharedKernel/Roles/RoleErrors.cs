using ControlHub.SharedKernel.Common.Errors;

namespace ControlHub.SharedKernel.Roles
{
    public static class RoleErrors
    {
        public static readonly Error RoleNameRequired =
            new("Role.NameRequired", "Role name is required.");

        public static readonly Error RoleAlreadyExists =
            new("Role.AlreadyExists", "A role with the same name already exists.");

        public static readonly Error RoleNotFound =
            new("Role.NotFound", "The specified role does not exist.");

        public static readonly Error RoleNameAlreadyExists =
            new("Role.NameAlreadyExists", "Role name already exists.");

        public static readonly Error PermissionAlreadyExists =
            new("Role.PermissionAlreadyExists", "Permission already exists in this role.");

        public static readonly Error PermissionNotFound =
            new("Role.PermissionNotFound", "Permission not found in this role.");

        public static readonly Error RoleInactive =
            new("Role.Inactive", "The role is inactive and cannot be modified.");

        public static readonly Error RoleUnexpectedError =
            new("Role.UnexpectedError", "An unexpected error occurred while processing the role.");

        public static readonly Error NoValidRolesCreated =
            new("Role.NoValidRolesCreated", "No valid roles were created. All provided roles were invalid or duplicate.");

        public static readonly Error PermissionRequired =
            new("Role.PermissionRequired", "Each role must have at least one permission assigned.");

        public static readonly Error InvalidPermissionReference =
            new("Role.InvalidPermissionReference", "One or more provided permission IDs do not exist.");

        public static readonly Error RoleValidationFailed =
            new("Role.ValidationFailed", "Role validation failed due to invalid input or missing data.");

        public static readonly Error PartialRoleCreation =
            new("Role.PartialRoleCreation", "Some roles were created successfully while others failed.");
    }
}