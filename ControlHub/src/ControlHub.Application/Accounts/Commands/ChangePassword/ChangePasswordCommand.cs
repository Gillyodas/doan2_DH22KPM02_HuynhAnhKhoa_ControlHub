using ControlHub.SharedKernel.Results;
using MediatR;

namespace ControlHub.Application.Accounts.Commands.ChangePassword
{
    public sealed record ChangePasswordCommand(Guid id, string curPassword, string newPassword) : IRequest<Result>;
}
