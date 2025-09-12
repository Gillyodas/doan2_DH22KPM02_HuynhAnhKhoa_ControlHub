using ControlHub.Domain.Accounts.ValueObjects;
using ControlHub.Infrastructure.Users;

namespace ControlHub.Infrastructure.Accounts
{
    public class AccountEntity
    {
        public Guid Id { get; set; }
        public Email Email { get; set; } = null!;
        public byte[] HashPassword { get; set; } = null!;
        public byte[] Salt { get; set; } = null!;
        public bool IsActive { get; set; }
        public bool IsDeleted { get; set; }

        // Navigation
        public UserEntity? User { get; set; }
    }
}
