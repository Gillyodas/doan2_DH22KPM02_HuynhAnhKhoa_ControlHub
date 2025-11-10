using ControlHub.Infrastructure.AccountRoles;
using ControlHub.Infrastructure.RolePermissions;

namespace ControlHub.Infrastructure.Roles
{
    public class RoleEntity
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = default!;
        public string Description { get; set; } = string.Empty;
        public bool IsActive { get; set; }

        // Navigation
        public ICollection<AccountRoleEntity> AccountRoles { get; set; } = new List<AccountRoleEntity>();
        public ICollection<RolePermissionEntity> RolePermissions { get; set; } = new List<RolePermissionEntity>();
    }
}