using ControlHub.Application.AccessControl.DTOs;
using ControlHub.SharedKernel.Results;
using MediatR;

namespace ControlHub.Application.AccessControl.Queries.GetUserRoles
{
    public record GetUserRolesQuery(Guid UserId) : IRequest<Result<List<RoleDto>>>;
}
