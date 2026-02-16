using ControlHub.Domain.Identity.Enums;
using ControlHub.SharedKernel.Common.Errors;

namespace ControlHub.Domain.Identity.Identifiers.Rules
{
    public class CustomIdentifierValidator : IIdentifierValidator
    {
        private readonly IdentifierValidationBuilder _builder;

        public IdentifierType Type => IdentifierType.Custom;

        public CustomIdentifierValidator(IdentifierValidationBuilder builder)
        {
            _builder = builder;
        }

        public (bool, string, Error?) ValidateAndNormalize(string raw)
        {
            var result = _builder.Validate(raw);

            if (result.IsFailure)
            {
                return (false, null!, Error.Validation("Custom.Invalid", result.Error.ToString()));
            }

            var normalized = _builder.Normalize(raw);
            return (true, normalized, null);
        }
    }
}
