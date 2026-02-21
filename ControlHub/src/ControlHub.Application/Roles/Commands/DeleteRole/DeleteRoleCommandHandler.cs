using ControlHub.Application.Accounts.Interfaces.Repositories;
using ControlHub.Application.Common.Persistence;
using ControlHub.Application.Roles.Interfaces.Repositories;
using ControlHub.SharedKernel.Results;
using ControlHub.SharedKernel.Roles;
using MediatR;
using Microsoft.Extensions.Logging;

namespace ControlHub.Application.Roles.Commands.DeleteRole
{
    public class DeleteRoleCommandHandler : IRequestHandler<DeleteRoleCommand, Result<Unit>>
    {
        private readonly IRoleRepository _roleRepository;
        private readonly IAccountRepository _accountRepository;
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<DeleteRoleCommandHandler> _logger;

        public DeleteRoleCommandHandler(
            IRoleRepository roleRepository,
            IAccountRepository accountRepository,
            IUnitOfWork unitOfWork,
            ILogger<DeleteRoleCommandHandler> logger)
        {
            _roleRepository = roleRepository;
            _accountRepository = accountRepository;
            _unitOfWork = unitOfWork;
            _logger = logger;
        }

        public async Task<Result<Unit>> Handle(DeleteRoleCommand request, CancellationToken ct)
        {
            _logger.LogInformation("{@LogCode} | RoleId: {RoleId}", RoleLogs.DeleteRole_Started, request.Id);

            var role = await _roleRepository.GetByIdAsync(request.Id, ct);
            if (role == null)
            {
                _logger.LogWarning("{@LogCode} | RoleId: {RoleId}", RoleLogs.DeleteRole_NotFound, request.Id);
                return Result<Unit>.Failure(RoleErrors.RoleNotFound);
            }

            var accounts = await _accountRepository.GetByRoleIdAsync(request.Id, ct);
            if (accounts.Any())
            {
                _logger.LogWarning("{@LogCode} | RoleId: {RoleId} | UsersCount: {UsersCount}",
                    RoleLogs.DeleteRole_InUse, request.Id, accounts.Count);
                return Result<Unit>.Failure(RoleErrors.RoleInUse);
            }

            role.Delete();

            await _unitOfWork.CommitAsync(ct);

            _logger.LogInformation("{@LogCode} | RoleId: {RoleId}", RoleLogs.DeleteRole_Success, request.Id);

            return Result<Unit>.Success(Unit.Value);
        }
    }
}
