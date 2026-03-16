using ControlHub.Domain.AccessControl.Entities;
using ControlHub.SharedKernel.Common.DTOs;
using ControlHub.SharedKernel.Results;
using MediatR;

namespace ControlHub.Application.AccessControl.Queries.SearchPermissions
{
    public sealed record SearchPermissionsQuery(
        int PageIndex,
        int PageSize,
        string[] Conditions
    ) : IRequest<Result<PagedResult<Permission>>>;
}
