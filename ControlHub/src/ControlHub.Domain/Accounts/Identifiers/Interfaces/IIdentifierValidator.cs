using ControlHub.Domain.Accounts.Enums;
using ControlHub.SharedKernel.Common.Errors;

namespace ControlHub.Application.Accounts.Identifiers.Interfaces
{
    public interface IIdentifierValidator
    {
        IdentifierType Type { get; }
        public (bool IsValid, string Normalized, Error? Error) ValidateAndNormalize(string rawValue);
    }
}
