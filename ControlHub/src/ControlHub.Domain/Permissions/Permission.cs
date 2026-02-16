using System.Text.RegularExpressions;
using ControlHub.SharedKernel.Permissions;
using ControlHub.SharedKernel.Results;

namespace ControlHub.Domain.Permissions
{
    public class Permission
    {
        // Regex cho phép: chữ thường, số, dấu chấm và dấu gạch dưới
        public const string PermissionCodeRegex = @"^[a-z0-9_]+\.[a-z0-9_]+$";

        public Guid Id { get; private set; }
        public string Code { get; private set; } = default!; // EF Core sẽ set giá trị này
        public string Description { get; private set; } = string.Empty;

        // Constructor rỗng cho EF Core (Bắt buộc)
        // EF Core dùng cái này để tạo object trước khi fill dữ liệu từ DB
        private Permission() { }

        // Constructor private cho Factory Method
        private Permission(Guid id, string code, string description)
        {
            Id = id;
            Code = code;
            Description = description;
        }

        // Factory Method
        public static Result<Permission> Create(Guid id, string code, string description)
        {
            if (id == Guid.Empty)
                return Result<Permission>.Failure(PermissionErrors.IdRequired);

            if (string.IsNullOrWhiteSpace(code))
                return Result<Permission>.Failure(PermissionErrors.PermissionCodeRequired);

            var normalizedCode = code.Trim().ToLowerInvariant();

            if (!Regex.IsMatch(normalizedCode, PermissionCodeRegex))
            {
                return Result<Permission>.Failure(PermissionErrors.InvalidPermissionFormat);
            }

            var normalizedDescription = description?.Trim() ?? string.Empty;

            return Result<Permission>.Success(new Permission(id, normalizedCode, normalizedDescription));
        }

        // Behavior: Update
        public Result Update(string code, string description)
        {
            if (string.IsNullOrWhiteSpace(code))
                return Result.Failure(PermissionErrors.PermissionCodeRequired);

            var normalizedCode = code.Trim().ToLowerInvariant();

            if (!Regex.IsMatch(normalizedCode, PermissionCodeRegex))
            {
                return Result.Failure(PermissionErrors.InvalidPermissionFormat);
            }

            Code = normalizedCode;
            Description = description?.Trim() ?? string.Empty;
            return Result.Success();
        }
    }
}
