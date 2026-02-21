using ControlHub.Application.Common.Persistence;
using ControlHub.Application.Roles.Interfaces.Repositories;
using ControlHub.SharedKernel.Results;
using ControlHub.SharedKernel.Roles;
using MediatR;
using Microsoft.Extensions.Logging;

namespace ControlHub.Application.Roles.Commands.UpdateRole
{
    public class UpdateRoleCommandHandler : IRequestHandler<UpdateRoleCommand, Result<Unit>>
    {
        private readonly IRoleRepository _roleRepository;
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<UpdateRoleCommandHandler> _logger;

        public UpdateRoleCommandHandler(
            IRoleRepository roleRepository,
            IUnitOfWork unitOfWork,
            ILogger<UpdateRoleCommandHandler> logger)
        {
            _roleRepository = roleRepository;
            _unitOfWork = unitOfWork;
            _logger = logger;
        }

        public async Task<Result<Unit>> Handle(UpdateRoleCommand request, CancellationToken ct)
        {
            _logger.LogInformation("{@LogCode} | RoleId: {RoleId}", RoleLogs.UpdateRole_Started, request.Id);

            var role = await _roleRepository.GetByIdAsync(request.Id, ct);
            if (role == null)
            {
                _logger.LogWarning("{@LogCode} | RoleId: {RoleId}", RoleLogs.UpdateRole_NotFound, request.Id);
                return Result<Unit>.Failure(RoleErrors.RoleNotFound);
            }

            // Check if name is changing and if it conflicts
            if (!string.Equals(role.Name, request.Name, StringComparison.OrdinalIgnoreCase))
            {
                var existingRole = await _roleRepository.GetByNameAsync(request.Name, ct);
                if (existingRole != null)
                {
                    _logger.LogWarning("{@LogCode} | RoleId: {RoleId} | Name: {Name}", RoleLogs.UpdateRole_Confict, request.Id, request.Name);
                    return Result<Unit>.Failure(RoleErrors.RoleNameAlreadyExists);
                }
            }

            var result = role.Update(request.Name, request.Description);
            if (result.IsFailure)
            {
                return Result<Unit>.Failure(result.Error);
            }

            await _unitOfWork.CommitAsync(ct);

            _logger.LogInformation("{@LogCode} | RoleId: {RoleId}", RoleLogs.UpdateRole_Success, request.Id);

            return Result<Unit>.Success(Unit.Value);
        }
    }
}
