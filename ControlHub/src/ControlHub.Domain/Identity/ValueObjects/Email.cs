using System.Text.RegularExpressions;
using ControlHub.SharedKernel.Accounts;
using ControlHub.SharedKernel.Results;

namespace ControlHub.Domain.Identity.ValueObjects
{
    public sealed class Email : IEquatable<Email>
    {
        private static readonly Regex _emailRegex =
            new(@"^(\w+(?:[.+\-]\w+)*)@(\w+(?:[.-]\w+)*\.[a-z]{2,})$",
                RegexOptions.Compiled | RegexOptions.IgnoreCase);

        public string Value { get; }

        private Email(string value) => Value = value;

        // Factory v?i validate
        public static Result<Email> Create(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return Result<Email>.Failure(AccountErrors.EmailRequired);

            if (!_emailRegex.IsMatch(value))
                return Result<Email>.Failure(AccountErrors.InvalidEmail);

            return Result<Email>.Success(new Email(value));
        }

        // Factory b? qua validate (ch? dùng khi materialize t? DB)
        public static Email UnsafeCreate(string value) => new(value);

        // Value Object equality
        public bool Equals(Email? other) => other is not null && Value == other.Value;
        public override bool Equals(object? obj) => obj is Email other && Equals(other);
        public override int GetHashCode() => Value.GetHashCode(StringComparison.OrdinalIgnoreCase);

        public override string ToString() => Value;
    }
}
