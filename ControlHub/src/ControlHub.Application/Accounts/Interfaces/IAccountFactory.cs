using ControlHub.Domain.Identity.Aggregates;
using ControlHub.Domain.Identity.Enums;
using ControlHub.SharedKernel.Results;

namespace ControlHub.Application.Accounts.Interfaces
{
    public interface IAccountFactory
    {
        Task<Result<Maybe<Account>>> CreateWithUserAndIdentifierAsync(
            Guid accountId,
            string identifierValue,
            IdentifierType identifierType,
            string rawPassword,
            Guid roleId,
            string? username = "No name",
            Guid? identifierConfigId = null);
    }
}
