using ControlHub.Infrastructure.Accounts;
using ControlHub.Infrastructure.Roles;

namespace ControlHub.Infrastructure.AccountRoles
{
    public class AccountRoleEntity
    {
        public Guid AccountId { get; set; }
        public Guid RoleId { get; set; }

        // Navigation
        public AccountEntity Account { get; set; } = default!;
        public RoleEntity Role { get; set; } = default!;
    }
}