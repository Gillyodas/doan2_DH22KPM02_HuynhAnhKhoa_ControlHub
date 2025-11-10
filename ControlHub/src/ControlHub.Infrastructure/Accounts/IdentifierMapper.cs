using ControlHub.Domain.Accounts.ValueObjects;

namespace ControlHub.Infrastructure.Accounts
{
    public static class IdentifierMapper
    {
        public static Identifier ToDomain(AccountIdentifierEntity entity)
        {
            return Identifier.Create(entity.Type, entity.Value, entity.NormalizedValue);
        }

        public static AccountIdentifierEntity ToEntity(Identifier vo, Guid accountId)
        {
            return new AccountIdentifierEntity
            {
                Id = Guid.NewGuid(),
                Type = vo.Type,
                Value = vo.Value,
                NormalizedValue = vo.NormalizedValue,
                AccountId = accountId
            };
        }
    }
}