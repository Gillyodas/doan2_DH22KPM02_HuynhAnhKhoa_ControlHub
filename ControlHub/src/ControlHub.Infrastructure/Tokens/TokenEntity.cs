using ControlHub.Domain.Tokens.Enums;
using ControlHub.Infrastructure.Accounts;

namespace ControlHub.Infrastructure.Tokens
{
    public class TokenEntity
    {
        public Guid Id { get; set; } // PK

        public Guid AccountId { get; set; } // FK

        public string Value { get; set; } = null!; // random string

        public TokenType Type { get; set; } // ResetPassword, VerifyEmail, RefreshToken...

        public DateTime ExpiredAt { get; set; }

        public bool IsUsed { get; set; }

        public DateTime CreatedAt { get; set; }

        // Navigation
        public AccountEntity Account { get; set; } = null!;
    }
}
