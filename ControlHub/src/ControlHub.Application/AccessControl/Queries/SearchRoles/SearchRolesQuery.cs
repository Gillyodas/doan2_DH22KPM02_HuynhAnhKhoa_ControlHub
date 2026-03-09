using ControlHub.SharedKernel.Common.DTOs;
using ControlHub.Domain.AccessControl.Aggregates;
using ControlHub.SharedKernel.Results;
using MediatR;

namespace ControlHub.Application.AccessControl.Queries.SearchRoles
{
    public sealed record SearchRolesQuery(int pageIndex, int pageSize, string[] conditions) : IRequest<Result<PagedResult<Role>>>;
}
