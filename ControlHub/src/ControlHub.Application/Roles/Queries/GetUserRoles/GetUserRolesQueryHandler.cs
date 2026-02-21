using ControlHub.Application.Roles.DTOs;
using ControlHub.Application.Roles.Interfaces.Repositories;
using ControlHub.SharedKernel.Results;
using ControlHub.SharedKernel.Roles;
using MediatR;
using Microsoft.Extensions.Logging;

namespace ControlHub.Application.Roles.Queries.GetUserRoles
{
    public class GetUserRolesQueryHandler : IRequestHandler<GetUserRolesQuery, Result<List<RoleDto>>>
    {
        private readonly IRoleQueries _roleQueries;
        private readonly ILogger<GetUserRolesQueryHandler> _logger;

        public GetUserRolesQueryHandler(IRoleQueries roleQueries, ILogger<GetUserRolesQueryHandler> logger)
        {
            _roleQueries = roleQueries;
            _logger = logger;
        }

        public async Task<Result<List<RoleDto>>> Handle(GetUserRolesQuery request, CancellationToken ct)
        {
            _logger.LogInformation("{@LogCode} | UserId: {UserId}", RoleLogs.GetUserRoles_Started, request.UserId);

            var roles = await _roleQueries.GetRolesByUserIdAsync(request.UserId, ct);

            _logger.LogInformation("{@LogCode} | UserId: {UserId} | Count: {Count}",
                RoleLogs.GetUserRoles_Success, request.UserId, roles.Count);

            return Result<List<RoleDto>>.Success(roles);
        }
    }
}
