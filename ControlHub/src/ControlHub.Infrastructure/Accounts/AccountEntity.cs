using ControlHub.Infrastructure.AccountRoles;
using ControlHub.Infrastructure.RolePermissions;
using ControlHub.Infrastructure.Users;
using ControlHub.Infrastructure.Tokens;

namespace ControlHub.Infrastructure.Accounts
{
    public class AccountEntity
    {
        public Guid Id { get; set; }
        public byte[] HashPassword { get; set; } = default!;
        public byte[] Salt { get; set; } = default!;
        public bool IsActive { get; set; }
        public bool IsDeleted { get; set; }

        // Navigation
        public ICollection<AccountIdentifierEntity> Identifiers { get; set; } = new List<AccountIdentifierEntity>();
        public UserEntity? User { get; set; }
        public ICollection<TokenEntity> Tokens { get; set; } = new List<TokenEntity>();
        public ICollection<AccountRoleEntity> AccountRoles { get; set; } = new List<AccountRoleEntity>();
    }
}