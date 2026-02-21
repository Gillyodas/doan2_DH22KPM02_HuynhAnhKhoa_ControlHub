using ControlHub.Application.Accounts.Interfaces.Repositories;
using ControlHub.Application.Common.Persistence;
using ControlHub.Application.Roles.Interfaces.Repositories;
using ControlHub.Application.Users.Interfaces.Repositories;
using ControlHub.SharedKernel.Accounts;
using ControlHub.SharedKernel.Results;
using ControlHub.SharedKernel.Roles;
using ControlHub.SharedKernel.Users;
using MediatR;
using Microsoft.Extensions.Logging;

namespace ControlHub.Application.Roles.Commands.AssignRoleToUser
{
    public class AssignRoleToUserCommandHandler : IRequestHandler<AssignRoleToUserCommand, Result<Unit>>
    {
        private readonly IUserRepository _userRepository;
        private readonly IAccountRepository _accountRepository;
        private readonly IRoleRepository _roleRepository;
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<AssignRoleToUserCommandHandler> _logger;

        public AssignRoleToUserCommandHandler(
            IUserRepository userRepository,
            IAccountRepository accountRepository,
            IRoleRepository roleRepository,
            IUnitOfWork unitOfWork,
            ILogger<AssignRoleToUserCommandHandler> logger)
        {
            _userRepository = userRepository;
            _accountRepository = accountRepository;
            _roleRepository = roleRepository;
            _unitOfWork = unitOfWork;
            _logger = logger;
        }

        public async Task<Result<Unit>> Handle(AssignRoleToUserCommand request, CancellationToken ct)
        {
            _logger.LogInformation("{@LogCode} | UserId: {UserId} | RoleId: {RoleId}",
                RoleLogs.AssignRole_Started, request.UserId, request.RoleId);

            // 1. Check User
            var user = await _userRepository.GetByIdAsync(request.UserId, ct);
            if (user == null)
            {
                _logger.LogWarning("{@LogCode} | UserId: {UserId}", RoleLogs.AssignRole_UserNotFound, request.UserId);
                return Result<Unit>.Failure(UserErrors.NotFound);
            }

            // 2. Check Role
            var role = await _roleRepository.GetByIdAsync(request.RoleId, ct);
            if (role == null)
            {
                _logger.LogWarning("{@LogCode} | RoleId: {RoleId}", RoleLogs.AssignRole_RoleNotFound, request.RoleId);
                return Result<Unit>.Failure(RoleErrors.RoleNotFound);
            }

            // 3. Get Account
            var account = await _accountRepository.GetWithoutUserByIdAsync(user.AccId, ct);
            if (account == null)
            {
                _logger.LogWarning("{@LogCode} | UserId: {UserId}", RoleLogs.AssignRole_AccountNotFound, request.UserId);
                return Result<Unit>.Failure(AccountErrors.AccountNotFound);
            }

            // 4. Assign Role
            var result = account.AttachRole(role);
            if (result.IsFailure)
            {
                // Log failure?
                return Result<Unit>.Failure(result.Error);
            }

            await _unitOfWork.CommitAsync(ct);

            _logger.LogInformation("{@LogCode} | UserId: {UserId} | RoleId: {RoleId}",
                RoleLogs.AssignRole_Success, request.UserId, request.RoleId);

            return Result<Unit>.Success(Unit.Value);
        }
    }
}
