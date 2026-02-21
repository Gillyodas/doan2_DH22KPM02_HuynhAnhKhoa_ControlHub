using ControlHub.Application.Common.Persistence;
using ControlHub.Application.Permissions.Interfaces.Repositories;
using ControlHub.Application.Roles.Interfaces.Repositories;
using ControlHub.SharedKernel.Results;
using ControlHub.SharedKernel.Roles;
using MediatR;
using Microsoft.Extensions.Logging;

namespace ControlHub.Application.Roles.Commands.SetRolePermissions
{
    public sealed class SetRolePermissionsCommandHandler : IRequestHandler<SetRolePermissionsCommand, Result>
    {
        private readonly IRoleRepository _roleRepository;
        private readonly IPermissionRepository _permissionRepository;
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<SetRolePermissionsCommandHandler> _logger;

        public SetRolePermissionsCommandHandler(
            IRoleRepository roleRepository,
            IPermissionRepository permissionRepository,
            IUnitOfWork unitOfWork,
            ILogger<SetRolePermissionsCommandHandler> logger)
        {
            _roleRepository = roleRepository;
            _permissionRepository = permissionRepository;
            _unitOfWork = unitOfWork;
            _logger = logger;
        }

        public async Task<Result> Handle(SetRolePermissionsCommand request, CancellationToken cancellationToken)
        {
            _logger.LogInformation("{@LogCode} | RoleId: {RoleId}", RoleLogs.SetPermissions_Started, request.RoleId);

            var role = await _roleRepository.GetByIdAsync(request.RoleId, cancellationToken);

            if (role == null)
            {
                _logger.LogWarning("{@LogCode} | RoleId: {RoleId}", RoleLogs.SetPermissions_RoleNotFound, request.RoleId);
                return Result.Failure(RoleErrors.RoleNotFound);
            }

            var permissions = (await _permissionRepository.GetByIdsAsync(request.PermissionIds, cancellationToken)).ToList();

            // Validate that all requested permissions exist
            // Retrieve unique requested IDs to handle accidental duplicates in request
            var uniqueRequestedIds = request.PermissionIds.Distinct().ToList();

            if (permissions.Count != uniqueRequestedIds.Count)
            {
                return Result.Failure(RoleErrors.InvalidPermissionReference);
            }

            // Clear existing permissions (This triggers 1 event)
            role.ClearPermissions();

            // Add new permissions using AddRangePermission (This triggers 1 event)
            var addResult = role.AddRangePermission(permissions);
            if (addResult.IsFailure)
            {
                return addResult;
            }

            await _unitOfWork.CommitAsync(cancellationToken);

            _logger.LogInformation("{@LogCode} | RoleId: {RoleId} | PermissionsCount: {Count}", RoleLogs.SetPermissions_Finished, request.RoleId, permissions.Count);

            return Result.Success();
        }
    }
}
