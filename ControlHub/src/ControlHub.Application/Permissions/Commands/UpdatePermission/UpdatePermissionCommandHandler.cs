using ControlHub.Application.Common.Persistence;
using ControlHub.Application.Permissions.Interfaces.Repositories;
using ControlHub.SharedKernel.Permissions;
using ControlHub.SharedKernel.Results;
using MediatR;
using Microsoft.Extensions.Logging;

namespace ControlHub.Application.Permissions.Commands.UpdatePermission
{
    public class UpdatePermissionCommandHandler : IRequestHandler<UpdatePermissionCommand, Result>
    {
        private readonly IPermissionRepository _permissionRepository;
        private readonly IUnitOfWork _uow;
        private readonly ILogger<UpdatePermissionCommandHandler> _logger;

        public UpdatePermissionCommandHandler(
            IPermissionRepository permissionRepository,
            IUnitOfWork uow,
            ILogger<UpdatePermissionCommandHandler> logger)
        {
            _permissionRepository = permissionRepository;
            _uow = uow;
            _logger = logger;
        }

        public async Task<Result> Handle(UpdatePermissionCommand request, CancellationToken cancellationToken)
        {
            _logger.LogInformation("{@LogCode} | PermissionId: {PermissionId}", PermissionLogs.UpdatePermission_Started, request.Id);

            var permission = await _permissionRepository.GetByIdAsync(request.Id, cancellationToken);

            if (permission == null)
            {
                _logger.LogWarning("{@LogCode} | PermissionId: {PermissionId}", PermissionLogs.UpdatePermission_NotFound, request.Id);
                return Result.Failure(PermissionErrors.PermissionNotFound);
            }

            var updateResult = permission.Update(request.Code, request.Description);

            if (updateResult.IsFailure)
            {
                _logger.LogWarning("{@LogCode} | PermissionId: {PermissionId}, Error: {Error}",
                    PermissionLogs.UpdatePermission_Started, // Re-using started or domain error? Reference used domain error but maybe simple started update info.
                    request.Id,
                    updateResult.Error.Code);
                return updateResult;
            }

            await _uow.CommitAsync(cancellationToken);

            _logger.LogInformation("{@LogCode} | PermissionId: {PermissionId}, Code: {Code}", PermissionLogs.UpdatePermission_Success, request.Id, request.Code);

            return Result.Success();
        }
    }
}
