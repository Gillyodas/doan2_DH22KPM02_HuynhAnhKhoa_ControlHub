using ControlHub.Application.Permissions.DTOs;
using ControlHub.SharedKernel.Results;
using MediatR;

namespace ControlHub.Application.Permissions.Commands.CreatePermissions
{
    public sealed record CreatePermissionsCommand(IEnumerable<CreatePermissionDto> Permissions) : IRequest<Result>;
}
