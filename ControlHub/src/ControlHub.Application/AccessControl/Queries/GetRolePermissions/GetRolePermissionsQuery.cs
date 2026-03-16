using ControlHub.Application.AccessControl.DTOs;
using ControlHub.Application.AccessControl.Interfaces.Repositories;
using ControlHub.SharedKernel.AccessControl.Roles;
using ControlHub.SharedKernel.Results;
using MediatR;
using Microsoft.Extensions.Logging;

namespace ControlHub.Application.AccessControl.Queries.GetRolePermissions
{
    public sealed record GetRolePermissionsQuery(Guid RoleId) : IRequest<Result<List<PermissionDto>>>;

    public sealed class GetRolePermissionsQueryHandler : IRequestHandler<GetRolePermissionsQuery, Result<List<PermissionDto>>>
    {
        private readonly IRoleRepository _roleRepository;
        private readonly ILogger<GetRolePermissionsQueryHandler> _logger;

        public GetRolePermissionsQueryHandler(IRoleRepository roleRepository, ILogger<GetRolePermissionsQueryHandler> logger)
        {
            _roleRepository = roleRepository;
            _logger = logger;
        }

        public async Task<Result<List<PermissionDto>>> Handle(GetRolePermissionsQuery request, CancellationToken cancellationToken)
        {
            _logger.LogInformation("{@LogCode} | RoleId: {RoleId}", RoleLogs.GetRolePermissions_Started, request.RoleId);

            var role = await _roleRepository.GetByIdAsync(request.RoleId, cancellationToken);

            if (role == null)
            {
                _logger.LogWarning("{@LogCode} | RoleId: {RoleId}", RoleLogs.GetRolePermissions_Started, request.RoleId); // RoleNotFound
                return Result<List<PermissionDto>>.Failure(RoleErrors.RoleNotFound);
            }

            var permissions = role.Permissions.Select(p => new PermissionDto(p.Id, p.Code, p.Description)).ToList();

            _logger.LogInformation("{@LogCode} | RoleId: {RoleId}, Count: {Count}", RoleLogs.GetRolePermissions_Success, request.RoleId, permissions.Count);

            return Result<List<PermissionDto>>.Success(permissions);
        }
    }
}
