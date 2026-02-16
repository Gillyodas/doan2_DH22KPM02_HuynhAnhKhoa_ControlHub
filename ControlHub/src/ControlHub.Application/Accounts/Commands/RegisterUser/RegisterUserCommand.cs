using ControlHub.Domain.Identity.Enums;
using ControlHub.SharedKernel.Results;
using MediatR;

namespace ControlHub.Application.Accounts.Commands.CreateAccount
{
    public sealed record RegisterUserCommand(string Value, IdentifierType Type, string Password, Guid? IdentifierConfigId = null) : IRequest<Result<Guid>>;
}
