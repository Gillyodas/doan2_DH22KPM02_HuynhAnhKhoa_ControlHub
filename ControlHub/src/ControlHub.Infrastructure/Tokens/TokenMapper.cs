using ControlHub.Domain.Tokens;

namespace ControlHub.Infrastructure.Tokens
{
    public static class TokenMapper
    {
        public static Token ToDomain(TokenEntity entity)
        {
            return Token.Rehydrate(
                entity.Id,
                entity.AccountId,
                entity.Value,
                entity.Type,
                entity.ExpiredAt,
                entity.IsUsed,
                entity.CreatedAt
            );
        }

        public static TokenEntity ToEntity(Token domain)
        {
            return new TokenEntity
            {
                Id = domain.Id,
                AccountId = domain.AccountId,
                Value = domain.Value,
                Type = domain.Type,
                ExpiredAt = domain.ExpiredAt,
                IsUsed = domain.IsUsed,
                CreatedAt = domain.CreatedAt
            };
        }
    }
}