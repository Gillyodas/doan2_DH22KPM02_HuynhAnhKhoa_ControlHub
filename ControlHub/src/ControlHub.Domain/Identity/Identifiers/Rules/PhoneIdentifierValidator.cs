using System.Text.RegularExpressions;
using ControlHub.Domain.Identity.Enums;
using ControlHub.SharedKernel.Common.Errors;
using ControlHub.SharedKernel.Indentifiers;

namespace ControlHub.Domain.Identity.Identifiers.Rules
{
    public class PhoneIdentifierValidator : IIdentifierValidator
    {
        public IdentifierType Type => IdentifierType.Phone;

        public (bool IsValid, string Normalized, Error? Error) ValidateAndNormalize(string rawValue)
        {
            if (string.IsNullOrWhiteSpace(rawValue))
                return (false, null!, IdentifierErrors.PhoneRequired);

            var trimmed = rawValue.Trim().Replace(" ", "").Replace("-", "");
            var regex = new Regex(@"^\+?[1-9]\d{7,14}$", RegexOptions.Compiled); // E.164

            if (!regex.IsMatch(trimmed))
                return (false, null!, IdentifierErrors.PhoneInvalidFormat);

            return (true, trimmed, null);
        }
    }
}
