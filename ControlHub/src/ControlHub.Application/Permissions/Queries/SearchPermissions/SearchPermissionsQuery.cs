using ControlHub.Application.Common.DTOs;
using ControlHub.Domain.Permissions;
using ControlHub.SharedKernel.Results;
using MediatR;

namespace ControlHub.Application.Permissions.Queries.SearchPermissions
{
    public sealed record SearchPermissionsQuery(
        int PageIndex,
        int PageSize,
        string[] Conditions
    ) : IRequest<Result<PagedResult<Permission>>>;
}
