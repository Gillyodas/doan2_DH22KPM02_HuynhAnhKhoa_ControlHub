using ControlHub.Application.AccessControl.DTOs;
using ControlHub.SharedKernel.Results;
using MediatR;

namespace ControlHub.Application.AccessControl.Commands.CreatePermissions
{
    public sealed record CreatePermissionsCommand(IEnumerable<CreatePermissionDto> Permissions) : IRequest<Result>;
}
