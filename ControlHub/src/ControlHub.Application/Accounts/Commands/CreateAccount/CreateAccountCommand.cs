using MediatR;
using ControlHub.SharedKernel.Results;

namespace ControlHub.Application.Accounts.Commands.CreateAccount
{
    public sealed record CreateAccountCommand(string Email, string Password) : IRequest<Result<Guid>>;
}
