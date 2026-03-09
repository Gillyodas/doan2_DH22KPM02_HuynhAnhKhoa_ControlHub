using ControlHub.Domain.Identity.Enums;

namespace ControlHub.Application.Identity.Interfaces
{
    public interface IAccountValidator
    {
        Task<bool> IdentifierIsExist(string Value, CancellationToken cancellationToken);
    }
}
