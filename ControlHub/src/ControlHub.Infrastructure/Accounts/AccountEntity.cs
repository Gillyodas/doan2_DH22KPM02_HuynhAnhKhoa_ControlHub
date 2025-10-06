using ControlHub.Domain.Accounts.ValueObjects;
using ControlHub.Infrastructure.Tokens;
using ControlHub.Infrastructure.Users;

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
    }
}
