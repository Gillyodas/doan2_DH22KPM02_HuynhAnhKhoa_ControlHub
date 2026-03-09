using ControlHub.SharedKernel.Results;
using MediatR;

namespace ControlHub.Application.Identity.Commands.ResetPassword
{
    public sealed record ResetPasswordCommand(string Token, string Password) : IRequest<Result>;
}
