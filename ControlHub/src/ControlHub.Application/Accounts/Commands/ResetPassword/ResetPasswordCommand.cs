using ControlHub.SharedKernel.Results;
using MediatR;

namespace ControlHub.Application.Accounts.Commands.ResetPassword
{
    public sealed record ResetPasswordCommand(string Token, string Password) : IRequest<Result>;
}
