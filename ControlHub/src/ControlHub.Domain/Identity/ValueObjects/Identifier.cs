using ControlHub.Domain.Identity.Enums;
using ControlHub.Domain.SharedKernel;
using ControlHub.SharedKernel.Results;

namespace ControlHub.Domain.Identity.ValueObjects
{
    public sealed class Identifier : ValueObject
    {
        public IdentifierType Type { get; }
        public string Name { get; }
        public string Value { get; }
        public string NormalizedValue { get; }
        public string Regex { get; }
        public bool IsDeleted { get; private set; }

        private Identifier(IdentifierType type, string name, string value, string normalizedValue, string regex)
        {
            Type = type;
            Name = name;
            Value = value;
            NormalizedValue = normalizedValue;
            Regex = regex ?? "";
        }

        private Identifier(IdentifierType type, string name, string value, string normalizedValue)
            : this(type, name, value, normalizedValue, "")
        {
        }

        public static Identifier Create(IdentifierType type, string value, string normalized)
            => new Identifier(type, GetDefaultName(type), value, normalized);

        public static Identifier CreateWithName(IdentifierType type, string name, string value, string normalized)
            => new Identifier(type, name, value, normalized);

        private static string GetDefaultName(IdentifierType type)
        {
            return type switch
            {
                IdentifierType.Email => "Email",
                IdentifierType.Phone => "Phone",
                IdentifierType.Username => "Username",
                _ => "Unknown"
            };
        }

        public Result<Identifier> UpdateNormalizedValue(string value)
        {
            return Result<Identifier>.Success(this);
        }

        protected override IEnumerable<object> GetEqualityComponents()
        {
            yield return Name;
            yield return NormalizedValue; // equality d?a trên name + normalized value
        }

        public override string ToString() => $"{Name}:{NormalizedValue}";

        public void Delete()
        {
            IsDeleted = true;
        }
    }
}
