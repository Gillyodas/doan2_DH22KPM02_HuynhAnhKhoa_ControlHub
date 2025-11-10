using ControlHub.SharedKernel.Common.Errors;

namespace ControlHub.SharedKernel.Permissions
{
    public static class PermissionErrors
    {
        public static readonly Error PermissionCodeRequired =
            new("Permission.CodeRequired", "Permission code is required.");

        public static readonly Error PermissionAlreadyExists =
            new("Permission.AlreadyExists", "A permission with the same code already exists.");

        public static readonly Error PermissionNotFound =
            new("Permission.NotFound", "The specified permission does not exist.");

        public static readonly Error InvalidPermissionFormat =
            new("Permission.InvalidFormat", "Permission code format is invalid.");

        public static readonly Error PermissionInUse =
            new("Permission.InUse", "The permission is currently assigned to a role and cannot be deleted.");

        public static readonly Error PermissionCodeAlreadyExists =
            new("Permission.CodeAlreadyExists", "Permission code already exists.");

        public static readonly Error PermissionUnexpectedError =
            new("Permission.UnexpectedError", "Unexpected error occurred while creating permissions.");

        public static readonly Error PermissionNotFoundValid =
            new("Permission.NotFoundValid", "No valid permissions found.");
    }
}