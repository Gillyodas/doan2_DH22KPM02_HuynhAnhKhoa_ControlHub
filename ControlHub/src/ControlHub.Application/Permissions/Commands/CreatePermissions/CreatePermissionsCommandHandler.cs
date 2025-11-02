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
        private readonly IPermissionCommands _permissionCommands;
        private readonly IPermissionQueries _permissionQueries;
        private readonly IUnitOfWork _uow;
        private readonly ILogger<CreatePermissionsCommandHandler> _logger;

        public CreatePermissionsCommandHandler(
            IPermissionCommands permissionCommands,
            IPermissionQueries permissionQueries,
            IUnitOfWork uow,
            ILogger<CreatePermissionsCommandHandler> logger)
        {
            _permissionCommands = permissionCommands;
            _permissionQueries = permissionQueries;
            _uow = uow;
            _logger = logger;
        }

        public async Task<Result> Handle(CreatePermissionsCommand request, CancellationToken cancellationToken)
        {
            _logger.LogInformation("{Code}: {Message}. Count={Count}",
            PermissionLogs.CreatePermissions_Started.Code,
            PermissionLogs.CreatePermissions_Started.Message,
            request.Permissions.Count());

            // Kiểm tra trùng Code
            var existing = await _permissionQueries.GetAllAsync(cancellationToken);
            var duplicates = request.Permissions
                .Where(p => existing.Any(e => e.Code.Equals(p.Code, StringComparison.OrdinalIgnoreCase)))
                .ToList();

            if (duplicates.Any())
            {
                _logger.LogWarning("{Code}: {Message}. Duplicates={Codes}",
                    PermissionLogs.CreatePermissions_Duplicate.Code,
                    PermissionLogs.CreatePermissions_Duplicate.Message,
                    string.Join(", ", duplicates.Select(d => d.Code)));

                return Result.Failure(PermissionErrors.PermissionCodeAlreadyExists);
            }

            var permissions = request.Permissions
                .Select(p => Permission.Create(Guid.NewGuid(), p.Code, p.Description))
                .ToList();

            await _permissionCommands.AddRangeAsync(permissions, cancellationToken);
            await _uow.CommitAsync(cancellationToken);

            _logger.LogInformation("{Code}: {Message}. Created={Count}",
                PermissionLogs.CreatePermissions_Success.Code,
                PermissionLogs.CreatePermissions_Success.Message,
                permissions.Count);

            return Result.Success();
        }
    }
}