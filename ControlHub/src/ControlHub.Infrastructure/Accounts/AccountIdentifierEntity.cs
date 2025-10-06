using ControlHub.Domain.Accounts.Enums;

namespace ControlHub.Infrastructure.Accounts
{
    public class AccountIdentifierEntity
    {
        public Guid Id { get; set; }
        public IdentifierType Type { get; set; }
        public string Value { get; set; } = null!;           // raw (for audit)
        public string NormalizedValue { get; set; } = null!; // used for lookup (lowercase/e164)
        public Guid AccountId { get; set; }

        // Navigation
        public AccountEntity Account { get; set; } = null!;
    }
}
