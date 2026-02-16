using ControlHub.Domain.Tokens.Enums;
using ControlHub.SharedKernel.Results;
using ControlHub.SharedKernel.Tokens;

namespace ControlHub.Domain.Tokens
{
    public class Token
    {
        public Guid Id { get; private set; }
        public Guid AccountId { get; private set; }
        public string Value { get; private set; } = default!; // EF Core set
        public TokenType Type { get; private set; }
        public DateTime ExpiredAt { get; private set; }
        public bool IsUsed { get; private set; }
        public DateTime CreatedAt { get; private set; }
        public bool IsRevoked { get; private set; }

        // Navigation Property (Optional)
        // Trong Domain, Token thu?ng không c?n gi? object Account.
        // Ch? c?n AccountId là d? d? d?nh danh ch? s? h?u.
        // N?u c?n, b?n có th? thêm: 
        // public Account Account { get; private set; } = null!;

        private Token() { } // for EF Core

        private Token(Guid id, Guid accountId, string value, TokenType type, DateTime expiredAt)
        {
            if (string.IsNullOrWhiteSpace(value))
                throw new ArgumentException("Token value is required", nameof(value));

            Id = id == Guid.Empty ? Guid.NewGuid() : id;
            AccountId = accountId;
            Value = value;
            Type = type;
            ExpiredAt = expiredAt;
            CreatedAt = DateTime.UtcNow;
            IsUsed = false;
            IsRevoked = false;
        }

        // Factory method
        public static Token Create(Guid accountId, string value, TokenType type, DateTime expiredAt)
            => new Token(Guid.NewGuid(), accountId, value, type, expiredAt);

        // Rehydrate
        public static Token Rehydrate(
            Guid id,
            Guid accountId,
            string value,
            TokenType type,
            DateTime expiredAt,
            bool isUsed,
            bool isRevoked,
            DateTime createdAt)
        {
            return new Token
            {
                Id = id,
                AccountId = accountId,
                Value = value,
                Type = type,
                ExpiredAt = expiredAt,
                IsUsed = isUsed,
                IsRevoked = isRevoked,
                CreatedAt = createdAt
            };
        }

        // Behavior
        public Result MarkAsUsed()
        {
            if (IsUsed)
                return Result.Failure(TokenErrors.TokenAlreadyUsed);

            if (DateTime.UtcNow > ExpiredAt)
                return Result.Failure(TokenErrors.TokenExpired);

            IsUsed = true;
            return Result.Success();
        }

        public bool IsValid() => !IsUsed && !IsRevoked && DateTime.UtcNow <= ExpiredAt;

        public Result Revoke()
        {
            if (IsRevoked)
                return Result.Failure(TokenErrors.TokenAlreadyRevoked);

            if (IsUsed)
                return Result.Failure(TokenErrors.TokenAlreadyUsed);

            IsRevoked = true;
            return Result.Success();
        }
    }
}
