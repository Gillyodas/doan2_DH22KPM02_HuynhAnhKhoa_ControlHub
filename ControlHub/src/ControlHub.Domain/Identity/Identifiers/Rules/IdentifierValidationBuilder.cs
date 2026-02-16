using System.Text.RegularExpressions;
using ControlHub.SharedKernel.Common.Errors;
using ControlHub.SharedKernel.Results;

namespace ControlHub.Domain.Identity.Identifiers.Rules
{
    public class IdentifierValidationBuilder
    {
        private readonly List<Func<string, Result>> _rules = new();
        private Func<string, string>? _normalizer;

        public IdentifierValidationBuilder Required(string? errorMessage = null)
        {
            _rules.Add(val => string.IsNullOrWhiteSpace(val)
            ? Result.Failure(Error.Validation("REQUIRED", errorMessage ?? "Value is required"))
            : Result.Success());

            return this;
        }

        public IdentifierValidationBuilder Length(int min, int max)
        {
            _rules.Add(val => val.Length < min || val.Length > max
                ? Result.Failure(Error.Validation("LENGTH", $"Length must be between {min} and {max}"))
                : Result.Success());
            return this;
        }

        public IdentifierValidationBuilder Pattern(string pattern, RegexOptions options = RegexOptions.None)
        {
            var regex = new Regex(pattern, options | RegexOptions.Compiled);
            _rules.Add(val => regex.IsMatch(val)
            ? Result.Failure(Error.Validation("PATTERN", "Invalid format"))
            : Result.Success());

            return this;
        }

        public IdentifierValidationBuilder Custom(Func<string, bool> predicate, string errorMessage)
        {
            _rules.Add(val => predicate(val)
            ? Result.Failure(Error.Validation("CUSTOM", errorMessage))
            : Result.Success());
            return this;
        }
        public IdentifierValidationBuilder Normalize(Func<string, string> normalizer)
        {
            _normalizer = normalizer;
            return this;
        }

        public CustomIdentifierValidator Build()
        {
            if (_normalizer == null)
                _normalizer = val => val.Trim().ToLowerInvariant(); // Default

            return new CustomIdentifierValidator(this);
        }

        internal Result Validate(string value)
        {
            foreach (var rule in _rules)
            {
                var result = rule(value);
                if (result.IsFailure) return result;
            }
            return Result.Success();
        }

        internal string Normalize(string value)
        {
            return _normalizer?.Invoke(value) ?? value;
        }
    }
}
