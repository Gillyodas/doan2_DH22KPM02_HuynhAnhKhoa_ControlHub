using ControlHub.Domain.Identity.Enums;
using ControlHub.Domain.Identity.ValueObjects;
using ControlHub.SharedKernel.Accounts;
using ControlHub.SharedKernel.Common.Errors;

namespace ControlHub.Domain.Identity.Identifiers.Rules
{
    public class EmailIdentifierValidator : IIdentifierValidator
    {
        public IdentifierType Type => IdentifierType.Email;
        public (bool IsValid, string Normalized, Error? Error) ValidateAndNormalize(string rawValue)
        {
            if (string.IsNullOrWhiteSpace(rawValue))
                return (false, null!, AccountErrors.EmailRequired);

            var trimmed = rawValue.Trim();
            var result = Email.Create(trimmed);

            if (!result.IsSuccess)
                return (false, null!, result.Error);

            var normalized = trimmed.ToLowerInvariant();
            return (true, normalized, null);
        }
    }
}
