using ControlHub.Application.Common.Persistence;
using ControlHub.Application.Permissions.Interfaces.Repositories;
using ControlHub.Domain.Permissions;
using ControlHub.SharedKernel.Permissions;
using ControlHub.SharedKernel.Results;
using MediatR;
using Microsoft.Extensions.Logging;

namespace ControlHub.Application.Permissions.Commands.CreatePermissions
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

            // 1. Kiểm tra trùng Code (Giữ nguyên)
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