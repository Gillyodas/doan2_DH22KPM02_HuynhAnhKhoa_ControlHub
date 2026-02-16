using ControlHub.Application.Common.DTOs;
using ControlHub.Application.Permissions.Interfaces.Repositories;
using ControlHub.Domain.Permissions;
using ControlHub.SharedKernel.Permissions;
using ControlHub.SharedKernel.Results;
using MediatR;
using Microsoft.Extensions.Logging;

namespace ControlHub.Application.Permissions.Queries.SearchPermissions
{
    public class SearchPermissionsQueryHandler : IRequestHandler<SearchPermissionsQuery, Result<PagedResult<Permission>>>
    {
        private readonly IPermissionQueries _permissionQueries;
        private readonly ILogger<SearchPermissionsQueryHandler> _logger;

        public SearchPermissionsQueryHandler(
            IPermissionQueries permissionQueries,
            ILogger<SearchPermissionsQueryHandler> logger)
        {
            _permissionQueries = permissionQueries;
            _logger = logger;
        }

        public async Task<Result<PagedResult<Permission>>> Handle(SearchPermissionsQuery request, CancellationToken cancellationToken)
        {
            _logger.LogInformation("{@LogCode} | PageIndex: {PageIndex} | PageSize: {PageSize}",
                PermissionLogs.SearchPermissions_Started,
                request.PageIndex,
                request.PageSize);

            var result = await _permissionQueries.SearchPaginationAsync(
                request.PageIndex,
                request.PageSize,
                request.Conditions,
                cancellationToken);

            _logger.LogInformation("{@LogCode} | TotalCount: {TotalCount}",
                PermissionLogs.SearchPermissions_Success,
                result.TotalCount);

            return Result<PagedResult<Permission>>.Success(result);
        }
    }
}
