using ControlHub.SharedKernel.Permissions;
using ControlHub.SharedKernel.Results;

namespace ControlHub.Domain.Permissions
{
    public class Permission
    {
        public Guid Id { get; private set; }
        public string Code { get; private set; }
        public string Description { get; private set; }

        private Permission() { }

        private Permission(Guid id, string code, string description)
        {
            if (id == Guid.Empty)
                throw new ArgumentException("Id is required", nameof(id));
            if (string.IsNullOrWhiteSpace(code))
                throw new ArgumentException("Permission code is required", nameof(code));

            Id = id;
            Code = code.Trim().ToLowerInvariant();
            Description = description?.Trim() ?? string.Empty;
        }

        // Factory methods
        public static Permission Create(Guid id, string code, string description)
            => new Permission(id, code, description);

        public static Permission Rehydrate(Guid id, string code, string description)
            => new Permission(id, code, description);

        // Behavior
        public Result Update(string code, string description)
        {
            if (string.IsNullOrWhiteSpace(code))
                return Result.Failure(PermissionErrors.PermissionCodeRequired);

            Code = code.Trim().ToLowerInvariant();
            Description = description?.Trim() ?? string.Empty;
            return Result.Success();
        }
    }
}