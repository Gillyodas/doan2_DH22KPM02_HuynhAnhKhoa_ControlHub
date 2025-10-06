using ControlHub.Domain.Tokens.Enums;
using ControlHub.SharedKernel.Results;
using ControlHub.SharedKernel.Tokens;

namespace ControlHub.Domain.Tokens
{
    public class Token
    {
        public Guid Id { get; private set; }
        public Guid AccountId { get; private set; }
        public string Value { get; private set; }
        public TokenType Type { get; private set; }
        public DateTime ExpiredAt { get; private set; }
        public bool IsUsed { get; private set; }
        public DateTime CreatedAt { get; private set; }

        private Token() { } // for rehydration

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
        }

        // Factory method để tạo token mới
        public static Token Create(Guid accountId, string value, TokenType type, DateTime expiredAt)
            => new Token(Guid.NewGuid(), accountId, value, type, expiredAt);

        // Rehydrate từ persistence
        public static Token Rehydrate(Guid id, Guid accountId, string value, TokenType type, DateTime expiredAt, bool isUsed, DateTime createdAt)
        {
            return new Token
            {
                Id = id,
                AccountId = accountId,
                Value = value,
                Type = type,
                ExpiredAt = expiredAt,
                IsUsed = isUsed,
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

        public bool IsValid() => !IsUsed && DateTime.UtcNow <= ExpiredAt;
    }
}