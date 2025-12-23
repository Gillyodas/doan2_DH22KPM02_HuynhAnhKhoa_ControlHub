using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ControlHub.Application.Common.DTOs;
using ControlHub.Application.Roles.Interfaces.Repositories;
using ControlHub.Domain.Roles;
using ControlHub.SharedKernel.Results;
using ControlHub.SharedKernel.Roles;
using MediatR;
using Microsoft.Extensions.Logging;

namespace ControlHub.Application.Roles.Queries.SearchRoles
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
            _logger.LogInformation("{Code}: {Message}. PageIndex={PageIndex}, PageSize={PageSize}",
                RoleLogs.SearchRoles_Started.Code,
                RoleLogs.SearchRoles_Started.Message,
                request.pageIndex,
                request.pageSize);

            var result = await _roleQueries.SearchPaginationAsync(request.pageIndex, request.pageSize, request.conditions, cancellationToken);

            _logger.LogInformation("{Code}: {Message}. TotalCount={TotalCount}",
                RoleLogs.SearchRoles_Success.Code,
                RoleLogs.SearchRoles_Success.Message,
                result.TotalCount);

            return Result<PagedResult<Role>>.Success(result);
        }
    }
}
