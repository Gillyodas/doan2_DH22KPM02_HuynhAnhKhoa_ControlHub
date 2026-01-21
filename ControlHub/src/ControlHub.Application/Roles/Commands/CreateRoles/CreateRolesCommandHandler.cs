using ControlHub.Application.Common.Persistence;
using ControlHub.Application.Roles.DTOs;
using ControlHub.Application.Permissions.Interfaces.Repositories;
using ControlHub.Application.Roles.Interfaces.Repositories;
using ControlHub.Domain.Common.Services;
using ControlHub.Domain.Permissions;
using ControlHub.Domain.Roles;
using ControlHub.SharedKernel.Permissions;
using ControlHub.SharedKernel.Results;
using ControlHub.SharedKernel.Roles;
using MediatR;
using Microsoft.Extensions.Logging;
using System.Linq;

namespace ControlHub.Application.Roles.Commands.CreateRoles
{
    public class CreateRolesCommandHandler : IRequestHandler<CreateRolesCommand, Result<PartialResult<Role, string>>>
    {
        private readonly IRoleRepository _roleRepository;
        private readonly IRoleQueries _roleQueries;
        private readonly IPermissionRepository _permissionRepository;
        private readonly CreateRoleWithPermissionsService _createRoleWithPermissionsService;
        private readonly ILogger<CreateRolesCommandHandler> _logger;
        private readonly IUnitOfWork _uow;

        public CreateRolesCommandHandler(
            IRoleRepository roleRepository,
            IRoleQueries roleQueries,
            IPermissionRepository permissionRepository,
            CreateRoleWithPermissionsService createRoleWithPermissionsService,
            ILogger<CreateRolesCommandHandler> logger,
            IUnitOfWork uow)
        {
            _roleRepository = roleRepository;
            _roleQueries = roleQueries;
            _permissionRepository = permissionRepository;
            _createRoleWithPermissionsService = createRoleWithPermissionsService;
            _logger = logger;
            _uow = uow;
        }

        public async Task<Result<PartialResult<Role, string>>> Handle(CreateRolesCommand request, CancellationToken ct)
        {
            _logger.LogInformation("{Code}: {Message}. Count={Count}",
                ControlHub.SharedKernel.Roles.RoleLogs.CreateRoles_Started.Code,
                ControlHub.SharedKernel.Roles.RoleLogs.CreateRoles_Started.Message,
                request.Roles?.Count() ?? 0);

            var existingNames = new HashSet<string>(
                (await _roleQueries.GetAllAsync(ct)).Select(r => r.Name.ToLowerInvariant()));

            var successes = new List<Role>();
            var failures = new List<string>();
            var dtosToProcess = new List<CreateRoleDto>();

            foreach (var dto in request.Roles)
            {
                if (existingNames.Contains(dto.Name.ToLowerInvariant()))
                {
                    _logger.LogWarning("{Code}: {Message}. Role={RoleName}",
                        ControlHub.SharedKernel.Roles.RoleLogs.CreateRoles_DuplicateNames.Code,
                        ControlHub.SharedKernel.Roles.RoleLogs.CreateRoles_DuplicateNames.Message,
                        dto.Name);

                    failures.Add($"{dto.Name}: {RoleErrors.RoleAlreadyExists.Code}");
                }
                else
                {
                    dtosToProcess.Add(dto);
                }
            }

            if (!dtosToProcess.Any() && !failures.Any())
            {
                _logger.LogWarning("{Code}: {Message}. IncomingCount={Count}",
                    ControlHub.SharedKernel.Roles.RoleLogs.CreateRoles_NoValidRole.Code,
                    ControlHub.SharedKernel.Roles.RoleLogs.CreateRoles_NoValidRole.Message,
                    request.Roles?.Count() ?? 0);

                return Result<PartialResult<Role, string>>.Failure(RoleErrors.NoValidRolesCreated);
            }

            if (!dtosToProcess.Any() && failures.Any())
            {
                return Result<PartialResult<Role, string>>.Success(PartialResult<Role, string>.Create(successes, failures));
            }

            var allRequiredPermissionIds = dtosToProcess
                .Where(r => r.PermissionIds != null)
                .SelectMany(r => r.PermissionIds!)
                .Distinct()
                .ToList();

            var allPermissions = await _permissionRepository.GetByIdsAsync(allRequiredPermissionIds, ct);
            var permissionMap = allPermissions.ToDictionary(p => p.Id);

            foreach (var dto in dtosToProcess)
            {
                if (dto.PermissionIds == null || !dto.PermissionIds.Any())
                {
                    _logger.LogWarning("{Code}: {Message}. Role={RoleName}",
                        ControlHub.SharedKernel.Roles.RoleLogs.CreateRoles_MissingPermissions.Code,
                        ControlHub.SharedKernel.Roles.RoleLogs.CreateRoles_MissingPermissions.Message,
                        dto.Name);

                    failures.Add($"{dto.Name}: {RoleErrors.PermissionRequired.Code}");
                    continue;
                }

                var rolePermissions = new List<Permission>();
                var missingPermissions = false;

                foreach (var pId in dto.PermissionIds)
                {
                    if (permissionMap.TryGetValue(pId, out var permissionInstance))
                    {
                        rolePermissions.Add(permissionInstance);
                    }
                    else
                    {
                        missingPermissions = true;
                        break;
                    }
                }

                if (missingPermissions)
                {
                    _logger.LogWarning("{Code}: {Message}. Role={RoleName}",
                        ControlHub.SharedKernel.Roles.RoleLogs.CreateRoles_NoValidPermissionFound.Code,
                        ControlHub.SharedKernel.Roles.RoleLogs.CreateRoles_NoValidPermissionFound.Message,
                        dto.Name);

                    failures.Add($"{dto.Name}: {ControlHub.SharedKernel.Permissions.PermissionErrors.PermissionNotFound.Code}");
                    continue;
                }

                var result = _createRoleWithPermissionsService.Handle(dto.Name, dto.Description, rolePermissions);

                if (result.IsSuccess)
                {
                    successes.Add(result.Value);
                    _logger.LogInformation("{Code}: {Message}. Role={RoleName}",
                        ControlHub.SharedKernel.Roles.RoleLogs.CreateRoles_RolePrepared.Code,
                        ControlHub.SharedKernel.Roles.RoleLogs.CreateRoles_RolePrepared.Message,
                        dto.Name);
                }
                else
                {
                    failures.Add($"{dto.Name}: {result.Error.Code}");
                    _logger.LogWarning("{Code}: {Message}. Role={RoleName} Error={ErrorCode}",
                        ControlHub.SharedKernel.Roles.RoleLogs.CreateRoles_RolePrepareFailed.Code,
                        ControlHub.SharedKernel.Roles.RoleLogs.CreateRoles_RolePrepareFailed.Message,
                        dto.Name,
                        result.Error.Code);
                }
            }

            if (successes.Any())
            {
                await _roleRepository.AddRangeAsync(successes, ct);
                await _uow.CommitAsync(ct);

                _logger.LogInformation("{Code}: {Message}. RolesCreated={Count}",
                    ControlHub.SharedKernel.Roles.RoleLogs.CreateRoles_Success.Code,
                    ControlHub.SharedKernel.Roles.RoleLogs.CreateRoles_Success.Message,
                    successes.Count);
            }
            else
            {
                _logger.LogInformation("{Code}: {Message}. No roles persisted.",
                    ControlHub.SharedKernel.Roles.RoleLogs.CreateRoles_NoPersist.Code,
                    ControlHub.SharedKernel.Roles.RoleLogs.CreateRoles_NoPersist.Message);
            }

            var partial = PartialResult<Role, string>.Create(successes, failures);

            if (!partial.Successes.Any() && failures.Any())
            {
                 // Handle case where nothing was created but we have failures
                 // Re-check logic here if needed. Current: return partial result.
            }

            return Result<PartialResult<Role, string>>.Success(partial);
        }
    }
}