using System.Text.RegularExpressions;
using ControlHub.SharedKernel.Accounts;
using ControlHub.SharedKernel.Results;

namespace ControlHub.Domain.Accounts.ValueObjects
{
    public sealed class Email : IEquatable<Email>
    {
        private static readonly Regex _emailRegex =
            // tối thiểu 2 ký tự sau dấu chấm, chỉ cho phép a-z
            new(@"^[^@\s]+@[^@\s]+\.[a-z]{2,}$", RegexOptions.Compiled | RegexOptions.IgnoreCase);

        public string Value { get; }

        private Email(string value) => Value = value;

        // Factory với validate
        public static Result<Email> Create(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return Result<Email>.Failure(AccountErrors.EmailRequired);

            if (!_emailRegex.IsMatch(value))
                return Result<Email>.Failure(AccountErrors.InvalidEmail);

            return Result<Email>.Success(new Email(value));
        }

        // Factory bỏ qua validate (chỉ dùng khi materialize từ DB)
        public static Email UnsafeCreate(string value) => new(value);

        // Value Object equality
        public bool Equals(Email? other) => other is not null && Value == other.Value;
        public override bool Equals(object? obj) => obj is Email other && Equals(other);
        public override int GetHashCode() => Value.GetHashCode(StringComparison.OrdinalIgnoreCase);

        public override string ToString() => Value;
    }
}
