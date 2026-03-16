using ControlHub.Application.AccessControl.Interfaces.Repositories;
using ControlHub.Domain.AccessControl.Aggregates;
using ControlHub.SharedKernel.AccessControl.Roles;
using ControlHub.SharedKernel.Common.DTOs;
using ControlHub.SharedKernel.Results;
using MediatR;
using Microsoft.Extensions.Logging;

namespace ControlHub.Application.AccessControl.Queries.SearchRoles
{
    public class SearchRolesQueryHandler : IRequestHandler<SearchRolesQuery, Result<PagedResult<Role>>>
    {
        private readonly IRoleQueries _roleQueries;
        private readonly ILogger<SearchRolesQueryHandler> _logger;

        public SearchRolesQueryHandler(IRoleQueries roleQueries, ILogger<SearchRolesQueryHandler> logger)
        {
            _roleQueries = roleQueries;
            _logger = logger;
        }

        public async Task<Result<PagedResult<Role>>> Handle(SearchRolesQuery request, CancellationToken cancellationToken)
        {
            _logger.LogInformation("{@LogCode} | PageIndex: {PageIndex} | PageSize: {PageSize}",
                RoleLogs.SearchRoles_Started,
                request.pageIndex,
                request.pageSize);

            var result = await _roleQueries.SearchPaginationAsync(request.pageIndex, request.pageSize, request.conditions, cancellationToken);

            _logger.LogInformation("{@LogCode} | TotalCount: {TotalCount}",
                RoleLogs.SearchRoles_Success,
                result.TotalCount);

            return Result<PagedResult<Role>>.Success(result);
        }
    }
}
