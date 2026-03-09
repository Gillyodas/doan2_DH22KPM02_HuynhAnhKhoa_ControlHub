using ControlHub.SharedKernel.Results;
using MediatR;

namespace ControlHub.Application.AccessControl.Commands.SetRolePermissions
{
    public sealed record SetRolePermissionsCommand(Guid RoleId, List<Guid> PermissionIds) : IRequest<Result>;
}
