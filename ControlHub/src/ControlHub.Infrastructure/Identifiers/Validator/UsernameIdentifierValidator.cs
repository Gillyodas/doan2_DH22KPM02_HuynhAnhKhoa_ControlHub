using System.Text.RegularExpressions;
using ControlHub.Application.Accounts.Identifiers.Interfaces;
using ControlHub.Domain.Accounts.Enums;
using ControlHub.SharedKernel.Common.Errors;
using ControlHub.SharedKernel.Indentifiers;

namespace ControlHub.Infrastructure.Identifiers.Validator
{
    public class UsernameIdentifierValidator : IIdentifierValidator
    {
        public IdentifierType Type => IdentifierType.Username;

        public (bool IsValid, string Normalized, Error? Error) ValidateAndNormalize(string rawValue)
        {
            if (string.IsNullOrWhiteSpace(rawValue))
                return (false, null!, IdentifierErrors.UsernameRequired);

            var trimmed = rawValue.Trim();
            if (trimmed.Length < 3 || trimmed.Length > 30)
                return (false, null!, IdentifierErrors.UsernameLengthInvalid);

            var regex = new Regex("^[a-zA-Z0-9._-]+$");
            if (!regex.IsMatch(trimmed))
                return (false, null!, IdentifierErrors.UsernameInvalidCharacters);

            var normalized = trimmed.ToLowerInvariant();
            return (true, normalized, null);
        }
    }
}
