using ControlHub.SharedKernel.Common.Errors;

namespace ControlHub.SharedKernel.Roles
{
    public static class RoleErrors
    {
        public static readonly Error RoleNameRequired =
            Error.Validation("Role.NameRequired", "Role name is required.");

        public static readonly Error RoleAlreadyExists =
            Error.Conflict("Role.AlreadyExists", "A role with the same name already exists.");

        public static readonly Error RoleNotFound =
            Error.NotFound("Role.NotFound", "The specified role does not exist.");

        public static readonly Error RoleNameAlreadyExists =
            Error.Conflict("Role.NameAlreadyExists", "Role name already exists.");

        public static readonly Error PermissionAlreadyExists =
            Error.Conflict("Role.PermissionAlreadyExists", "Permission already exists in this role.");

        public static readonly Error PermissionNotFound =
            Error.NotFound("Role.PermissionNotFound", "Permission not found in this role.");

        public static readonly Error RoleInactive =
            Error.Conflict("Role.Inactive", "The role is inactive and cannot be modified.");

        public static readonly Error RoleUnexpectedError =
            Error.Failure("Role.UnexpectedError", "An unexpected error occurred while processing the role.");

        public static readonly Error NoValidRolesCreated =
            Error.Validation("Role.NoValidRolesCreated", "No valid roles were created. All provided roles were invalid or duplicate.");

        public static readonly Error PermissionRequired =
            Error.Validation("Role.PermissionRequired", "Each role must have at least one permission assigned.");

        public static readonly Error InvalidPermissionReference =
            Error.NotFound("Role.InvalidPermissionReference", "One or more provided permission IDs do not exist.");

        public static readonly Error RoleValidationFailed =
            Error.Validation("Role.ValidationFailed", "Role validation failed due to invalid input or missing data.");

        public static readonly Error PartialRoleCreation =
            Error.Conflict("Role.PartialRoleCreation", "Some roles were created successfully while others failed.");

        public static readonly Error RoleIdRequired =
            Error.Validation("Role.IdRequired", "Role Id is required.");

        public static readonly Error AllPermissionsAlreadyExist =
            Error.Conflict("Role.Permissions.NoNew", "No new permissions were added. All specified permissions already exist in this role.");

        public static readonly Error InvalidRoleIdFormat =
            Error.Validation("Role.InvalidId", "The provided Role ID is not in a valid GUID format.");

        public static readonly Error RoleInUse =
            Error.Conflict("Role.InUse", "Cannot delete role because it is currently assigned to users.");
    }
}
