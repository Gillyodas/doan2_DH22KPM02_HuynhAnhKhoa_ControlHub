using ControlHub.SharedKernel.Common.Errors;

namespace ControlHub.SharedKernel.Permissions
{
    public static class PermissionErrors
    {
        public static readonly Error PermissionCodeRequired =
            Error.Validation("Permission.CodeRequired", "Permission code is required.");

        public static readonly Error PermissionAlreadyExists =
            Error.Conflict("Permission.AlreadyExists", "A permission with the same code already exists.");

        public static readonly Error PermissionNotFound =
            Error.NotFound("Permission.NotFound", "The specified permission does not exist.");

        public static readonly Error InvalidPermissionFormat =
            Error.Validation("Permission.InvalidFormat", "Permission code format is invalid.");

        public static readonly Error PermissionInUse =
            Error.Conflict("Permission.InUse", "The permission is currently assigned to a role and cannot be deleted.");

        public static readonly Error PermissionCodeAlreadyExists =
            Error.Conflict("Permission.CodeAlreadyExists", "Permission code already exists.");

        public static readonly Error PermissionUnexpectedError =
            Error.Failure("Permission.UnexpectedError", "Unexpected error occurred while creating permissions.");

        public static readonly Error PermissionNotFoundValid =
            Error.NotFound("Permission.NotFoundValid", "No valid permissions found.");

        public static readonly Error IdRequired =
            Error.Validation("Permission.Id.Required", "Id is required.");
    }
}
