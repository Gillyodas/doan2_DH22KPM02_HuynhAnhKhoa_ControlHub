using ControlHub.Domain.Accounts.Enums;
using ControlHub.SharedKernel.Results;
using MediatR;

namespace ControlHub.Application.Accounts.Commands.CreateAccount
{
    public sealed record CreateAccountCommand(string Value, IdentifierType Type, string Password) : IRequest<Result<Guid>>;
}
