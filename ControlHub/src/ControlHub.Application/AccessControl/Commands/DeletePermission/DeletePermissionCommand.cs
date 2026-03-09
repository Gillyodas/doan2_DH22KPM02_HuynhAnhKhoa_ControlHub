using ControlHub.SharedKernel.Results;
using MediatR;

namespace ControlHub.Application.AccessControl.Commands.DeletePermission
{
    public sealed record DeletePermissionCommand(Guid Id) : IRequest<Result>;
}
