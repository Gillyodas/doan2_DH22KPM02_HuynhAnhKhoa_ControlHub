using ControlHub.Application.AccessControl.Interfaces.Repositories;
using ControlHub.Application.Common.Persistence;
using ControlHub.Domain.AccessControl.Entities;
using ControlHub.SharedKernel.AccessControl.Permissions;
using ControlHub.SharedKernel.Results;
using MediatR;
using Microsoft.Extensions.Logging;

namespace ControlHub.Application.AccessControl.Commands.CreatePermissions
{
    public class CreatePermissionsCommandHandler : IRequestHandler<CreatePermissionsCommand, Result>
    {
        private readonly IPermissionRepository _permissionRepository;
        private readonly IPermissionQueries _permissionQueries;
        private readonly IUnitOfWork _uow;
        private readonly ILogger<CreatePermissionsCommandHandler> _logger;

        public CreatePermissionsCommandHandler(
            IPermissionRepository permissionRepository,
            IPermissionQueries permissionQueries,
            IUnitOfWork uow,
            ILogger<CreatePermissionsCommandHandler> logger)
        {
            _permissionRepository = permissionRepository;
            _permissionQueries = permissionQueries;
            _uow = uow;
            _logger = logger;
        }

        public async Task<Result> Handle(CreatePermissionsCommand request, CancellationToken cancellationToken)
        {
            _logger.LogInformation("{@LogCode} | Count: {Count}",
                PermissionLogs.CreatePermissions_Started,
                request.Permissions.Count());

            // 1. Ki?m tra trùng Code (Gi? nguyên)
            var existing = await _permissionQueries.GetAllAsync(cancellationToken);
            var duplicates = request.Permissions
                .Where(p => existing.Any(e => e.Code.Equals(p.Code, StringComparison.OrdinalIgnoreCase)))
                .ToList();

            if (duplicates.Any())
            {
                _logger.LogWarning("{@LogCode} | Duplicates: {Codes}",
                    PermissionLogs.CreatePermissions_Duplicate,
                    string.Join(", ", duplicates.Select(d => d.Code)));

                return Result.Failure(PermissionErrors.PermissionCodeAlreadyExists);
            }

            var validPermissions = new List<Permission>();

            foreach (var p in request.Permissions)
            {
                var permissionResult = Permission.Create(Guid.NewGuid(), p.Code, p.Description);

                if (permissionResult.IsFailure)
                {
                    _logger.LogWarning("{@LogCode} | Code: {Code}, Error: {Error}",
                        PermissionLogs.CreatePermissions_DomainError,
                        p.Code,
                        permissionResult.Error.Code);

                    return Result.Failure(permissionResult.Error);
                }

                validPermissions.Add(permissionResult.Value);
            }

            await _permissionRepository.AddRangeAsync(validPermissions, cancellationToken);
            await _uow.CommitAsync(cancellationToken);

            _logger.LogInformation("{@LogCode} | Created: {Count}",
                PermissionLogs.CreatePermissions_Success,
                validPermissions.Count);

            return Result.Success();
        }
    }
}
