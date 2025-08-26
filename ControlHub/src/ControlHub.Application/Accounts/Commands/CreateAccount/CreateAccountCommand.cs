using MediatR;

namespace ControlHub.Application.Accounts.Commands.CreateAccount
{
    public record CreateAccountCommand(string Email, string Password) : IRequest<Guid>;
}
