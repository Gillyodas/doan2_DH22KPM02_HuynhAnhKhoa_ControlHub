using ControlHub.Domain.Identity.Enums;
using ControlHub.SharedKernel.Results;
using MediatR;

namespace ControlHub.Application.Accounts.Commands.AddIdentifier
{
    public sealed record AddIdentifierCommand(string value, IdentifierType type, Guid id) : IRequest<Result>;
}
