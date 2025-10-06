using System.Text.RegularExpressions;
using ControlHub.Application.Accounts.Identifiers.Interfaces;
using ControlHub.Domain.Accounts.Enums;
using ControlHub.SharedKernel.Common.Errors;
using ControlHub.SharedKernel.Indentifiers;

namespace ControlHub.Infrastructure.Identifiers.Validator
{
    public class PhoneIdentifierValidator : IIdentifierValidator
    {
        public IdentifierType Type => IdentifierType.Phone;

        public (bool IsValid, string Normalized, Error? Error) ValidateAndNormalize(string rawValue)
        {
            if (string.IsNullOrWhiteSpace(rawValue))
                return (false, null!, IdentifierErrors.PhoneRequired);

            var trimmed = rawValue.Trim().Replace(" ", "").Replace("-", "");

            var regex = new Regex(@"^\+?[1-9]\d{7,14}$");
            // E.164: max 15 digits, min 8 digits
            if (!regex.IsMatch(trimmed))
                return (false, null!, IdentifierErrors.PhoneInvalidFormat);

            return (true, trimmed, null);
        }
    }
}
