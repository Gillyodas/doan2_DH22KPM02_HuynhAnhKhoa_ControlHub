using ControlHub.Application.Common.DTOs;
using ControlHub.Domain.Roles;
using ControlHub.SharedKernel.Results;
using MediatR;

namespace ControlHub.Application.Roles.Queries.SearchRoles
{
    public sealed record SearchRolesQuery(int pageIndex, int pageSize, string[] conditions) : IRequest<Result<PagedResult<Role>>>;
}
