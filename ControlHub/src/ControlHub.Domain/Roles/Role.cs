using ControlHub.Domain.Permissions;
using ControlHub.SharedKernel.Results;
using ControlHub.SharedKernel.Roles;
// using ControlHub.SharedKernel.Roles; // (Gi? s? b?n có RoleErrors ? dây)

namespace ControlHub.Domain.Roles
{
    public class Role
    {
        public Guid Id { get; private set; }
        public string Name { get; private set; }
        public string Description { get; private set; }
        public bool IsActive { get; private set; }
        public bool IsDeleted { get; private set; }

        private readonly List<Permission> _permissions = new();

        public IReadOnlyCollection<Permission> Permissions => _permissions.AsReadOnly();

        // Navigation Property (Optional): N?u b?n mu?n truy c?p ngu?c l?i t? Role -> Accounts
        // public IReadOnlyCollection<Account> Accounts => _accounts.AsReadOnly();
        // private readonly List<Account> _accounts = new();

        // Constructor cho EF Core
        private Role() { }

        private Role(Guid id, string name, string description, bool isActive)
        {
            Id = id;
            Name = name;
            Description = description;
            IsActive = isActive;
        }

        public static Role Create(Guid id, string name, string description)
        {
            if (id == Guid.Empty) throw new ArgumentException("Id is required", nameof(id));
            if (string.IsNullOrWhiteSpace(name)) throw new ArgumentException("Name is required", nameof(name));

            return new Role(id, name.Trim(), description?.Trim() ?? string.Empty, true);
        }

        // Logic Update
        public Result Update(string name, string description)
        {
            if (string.IsNullOrWhiteSpace(name))
                return Result.Failure(RoleErrors.RoleNameRequired);

            Name = name.Trim();
            Description = description?.Trim() ?? string.Empty;
            return Result.Success();
        }

        // Logic Add Permission
        public Result AddPermission(Permission permission)
        {
            if (permission == null)
                return Result.Failure(RoleErrors.PermissionNotFound); // Ho?c l?i Null

            // Ki?m tra trùng l?p trong list hi?n t?i
            if (_permissions.Any(p => p.Code == permission.Code))
            {
                return Result.Failure(RoleErrors.PermissionAlreadyExists);
            }

            _permissions.Add(permission);
            return Result.Success();
        }

        public Result<PartialResult<Permission, string>> AddRangePermission(IEnumerable<Permission> permissionsToAdd)
        {
            var successes = new List<Permission>();
            var failures = new List<string>();

            var existingCodes = _permissions.Select(p => p.Code).ToHashSet();

            foreach (var per in permissionsToAdd)
            {
                if (existingCodes.Contains(per.Code))
                {
                    failures.Add($"{per.Code}: is already exist in role: {this.Name}");
                }
                else
                {
                    _permissions.Add(per);
                    successes.Add(per);
                    existingCodes.Add(per.Code);
                }
            }

            var partial = PartialResult<Permission, string>.Create(successes, failures);

            if (!partial.Successes.Any() && partial.Failures.Any())
            {
                return Result<PartialResult<Permission, string>>.Failure(RoleErrors.AllPermissionsAlreadyExist);
            }

            return Result<PartialResult<Permission, string>>.Success(partial);
        }

        public void ClearPermissions()
        {
            _permissions.Clear();
        }

        public void Deactivate() => IsActive = false;
        public void Activate() => IsActive = true;
        public void Delete() => IsDeleted = true;
    }
}
