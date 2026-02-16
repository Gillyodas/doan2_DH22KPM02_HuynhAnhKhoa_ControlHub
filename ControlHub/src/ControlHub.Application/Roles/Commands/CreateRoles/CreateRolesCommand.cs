using ControlHub.Application.Roles.DTOs;
using ControlHub.Domain.AccessControl.Aggregates;
using ControlHub.SharedKernel.Results;
using MediatR;

namespace ControlHub.Application.Roles.Commands.CreateRoles
{
    public sealed record CreateRolesCommand(IEnumerable<CreateRoleDto> Roles) : IRequest<Result<PartialResult<Role, string>>>;
}
