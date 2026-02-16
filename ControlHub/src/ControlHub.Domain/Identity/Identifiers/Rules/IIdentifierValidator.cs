using ControlHub.Domain.Identity.Enums;
using ControlHub.SharedKernel.Common.Errors;

namespace ControlHub.Domain.Identity.Identifiers.Rules
{
    public interface IIdentifierValidator
    {
        IdentifierType Type { get; }
        (bool IsValid, string Normalized, Error? Error) ValidateAndNormalize(string rawValue);
    }
}
