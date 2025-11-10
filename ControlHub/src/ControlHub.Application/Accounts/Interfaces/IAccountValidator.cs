using ControlHub.Domain.Accounts.Enums;

namespace ControlHub.Application.Accounts.Interfaces
{
    public interface IAccountValidator
    {
        Task<bool> IdentifierIsExist(string Value, IdentifierType Type, CancellationToken cancellationToken);
    }
}
