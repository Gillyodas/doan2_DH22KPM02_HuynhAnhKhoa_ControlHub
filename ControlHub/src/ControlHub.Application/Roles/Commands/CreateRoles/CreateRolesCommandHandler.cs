using ControlHub.Application.Common.Persistence;
using ControlHub.Application.Permissions.Interfaces.Repositories;
using ControlHub.Application.Roles.Interfaces.Repositories;
using ControlHub.Domain.Common.Services;
using ControlHub.Domain.Roles;
using ControlHub.SharedKernel.Permissions;
using ControlHub.SharedKernel.Results;
using ControlHub.SharedKernel.Roles;
using MediatR;
using Microsoft.Extensions.Logging;

namespace ControlHub.Application.Roles.Commands.CreateRoles
{
    public class CreateRolesCommandHandler : IRequestHandler<CreateRolesCommand, Result<PartialResult<Role, string>>>
    {
        private readonly IRoleCommands _roleCommands;
        private readonly IRoleQueries _roleQueries;
        private readonly IPermissionQueries _permissionQueries;
        private readonly CreateRoleWithPermissionsService _createRoleWithPermissionsService;
        private readonly ILogger<CreateRolesCommandHandler> _logger;
        private readonly IUnitOfWork _uow;

        public CreateRolesCommandHandler(
            IRoleCommands roleCommands,
            IRoleQueries roleQueries,
            IPermissionQueries permissionQueries,
            CreateRoleWithPermissionsService createRoleWithPermissionsService,
            ILogger<CreateRolesCommandHandler> logger,
            IUnitOfWork uow)
        {
            _roleCommands = roleCommands;
            _roleQueries = roleQueries;
            _permissionQueries = permissionQueries;
            _createRoleWithPermissionsService = createRoleWithPermissionsService;
            _logger = logger;
            _uow = uow;
        }

        public async Task<Result<PartialResult<Role, string>>> Handle(CreateRolesCommand request, CancellationToken ct)
        {
            _logger.LogInformation("{Code}: {Message}. Count={Count}",
                RoleLogs.CreateRoles_Started.Code,
                RoleLogs.CreateRoles_Started.Message,
                request.Roles?.Count() ?? 0);

            // Load existing role names once (case-insensitive)
            var existingNames = new HashSet<string>(
                (await _roleQueries.GetAllAsync(ct)).Select(r => r.Name.ToLowerInvariant()));

            // Filter out duplicates coming from DB
            var validDtos = request.Roles
                .Where(r => !existingNames.Contains(r.Name.ToLowerInvariant()))
                .ToList();

            // If nothing valid, return failure early
            if (!validDtos.Any())
            {
                _logger.LogWarning("{Code}: {Message}. IncomingCount={Count}",
                    RoleLogs.CreateRoles_NoValidRole.Code,
                    RoleLogs.CreateRoles_NoValidRole.Message,
                    request.Roles?.Count() ?? 0);

                return Result<PartialResult<Role, string>>.Failure(RoleErrors.NoValidRolesCreated);
            }

            var successes = new List<Role>();
            var failures = new List<string>();

            // Iterate sequentially to keep EF DbContext safety and simple error tracing
            foreach (var dto in validDtos)
            {
                // If client didn't supply permission ids, treat as validation failure (or decide to allow empty)
                if (dto.PermissionIds == null || !dto.PermissionIds.Any())
                {
                    _logger.LogWarning("{Code}: {Message}. Role={RoleName}",
                        RoleLogs.CreateRoles_MissingPermissions.Code,
                        RoleLogs.CreateRoles_MissingPermissions.Message,
                        dto.Name);

                    failures.Add($"{dto.Name}: {RoleErrors.PermissionRequired.Code}");
                    continue;
                }

                // Load permissions from DB (application layer does I/O)
                var validPermissions = (await _permissionQueries.GetByIdsAsync(dto.PermissionIds, ct)).ToList();

                if (!validPermissions.Any())
                {
                    _logger.LogWarning("{Code}: {Message}. Role={RoleName} ProvidedCount={Count}",
                        RoleLogs.CreateRoles_NoValidPermissionFound.Code,
                        RoleLogs.CreateRoles_NoValidPermissionFound.Message,
                        dto.Name,
                        dto.PermissionIds.Count());

                    failures.Add($"{dto.Name}: {PermissionErrors.PermissionNotFound.Code}");
                    continue;
                }

                // Delegate pure domain composition to service (synchronous domain work assumed)
                var result = _createRoleWithPermissionsService.Handle(dto.Name, dto.Description, validPermissions);

                if (result.IsSuccess)
                {
                    successes.Add(result.Value);
                    _logger.LogInformation("{Code}: {Message}. Role={RoleName}",
                        RoleLogs.CreateRoles_RolePrepared.Code,
                        RoleLogs.CreateRoles_RolePrepared.Message,
                        dto.Name);
                }
                else
                {
                    failures.Add($"{dto.Name}: {result.Error.Code}");
                    _logger.LogWarning("{Code}: {Message}. Role={RoleName} Error={ErrorCode}",
                        RoleLogs.CreateRoles_RolePrepareFailed.Code,
                        RoleLogs.CreateRoles_RolePrepareFailed.Message,
                        dto.Name,
                        result.Error.Code);
                }
            }

            // Persist successful roles (if any)
            if (successes.Any())
            {
                await _roleCommands.AddRangeAsync(successes, ct);
                await _uow.CommitAsync(ct);

                _logger.LogInformation("{Code}: {Message}. RolesCreated={Count}",
                    RoleLogs.CreateRoles_Success.Code,
                    RoleLogs.CreateRoles_Success.Message,
                    successes.Count);
            }
            else
            {
                _logger.LogInformation("{Code}: {Message}. No roles persisted.",
                    RoleLogs.CreateRoles_NoPersist.Code,
                    RoleLogs.CreateRoles_NoPersist.Message);
            }

            // Build partial result and return wrapped in Result<T>
            var partial = PartialResult<Role, string>.Create(successes, failures);

            // If nothing succeeded, return Failure to reflect no persisted outcome
            if (!partial.Successes.Any())
                return Result<PartialResult<Role, string>>.Failure(RoleErrors.NoValidRolesCreated);

            return Result<PartialResult<Role, string>>.Success(partial);
        }
    }
}