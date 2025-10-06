using ControlHub.Domain.Accounts.Enums;
using ControlHub.Domain.Common;

namespace ControlHub.Domain.Accounts.ValueObjects
{
    public sealed class Identifier : ValueObject
    {
        public IdentifierType Type { get; }
        public string Value { get; }
        public string NormalizedValue { get; }

        private Identifier(IdentifierType type, string value, string normalizedValue)
        {
            Type = type;
            Value = value;
            NormalizedValue = normalizedValue;
        }

        public static Identifier Create(IdentifierType type, string value, string normalized)
            => new Identifier(type, value, normalized);

        protected override IEnumerable<object> GetEqualityComponents()
        {
            yield return Type;
            yield return NormalizedValue; // equality dựa trên type + normalized value
        }

        public override string ToString() => $"{Type}:{NormalizedValue}";
    }
}