using ControlHub.Application.Accounts.Interfaces.Repositories;
using ControlHub.Application.Common.Persistence;
using ControlHub.Application.Users.Interfaces.Repositories;
using ControlHub.SharedKernel.Accounts;
using ControlHub.SharedKernel.Results;
using ControlHub.SharedKernel.Users;
using MediatR;

using Microsoft.Extensions.Logging;

namespace ControlHub.Application.Users.Commands.DeleteUser
{
    public class DeleteUserCommandHandler : IRequestHandler<DeleteUserCommand, Result<Unit>>
    {
        private readonly IUserRepository _userRepository;
        private readonly IAccountRepository _accountRepository;
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<DeleteUserCommandHandler> _logger;

        public DeleteUserCommandHandler(
            IUserRepository userRepository,
            IAccountRepository accountRepository,
            IUnitOfWork unitOfWork,
            ILogger<DeleteUserCommandHandler> logger)
        {
            _userRepository = userRepository;
            _accountRepository = accountRepository;
            _unitOfWork = unitOfWork;
            _logger = logger;
        }

        public async Task<Result<Unit>> Handle(DeleteUserCommand request, CancellationToken ct)
        {
            _logger.LogInformation("{@LogCode} | UserId: {UserId}", UserLogs.DeleteUser_Started, request.Id);

            var user = await _userRepository.GetByIdAsync(request.Id, ct);
            if (user == null)
            {
                _logger.LogWarning("{@LogCode} | UserId: {UserId}", UserLogs.DeleteUser_NotFound, request.Id);
                return Result<Unit>.Failure(UserErrors.NotFound);
            }

            var account = await _accountRepository.GetWithoutUserByIdAsync(user.AccId, ct);
            if (account == null)
            {
                return Result<Unit>.Failure(AccountErrors.AccountNotFound);
            }

            // Soft delete Account (cascades to tokens, identifiers)
            account.Delete();

            // Soft delete User (explicitly just in case Account doesn't have User loaded)
            user.Delete();

            await _unitOfWork.CommitAsync(ct);

            _logger.LogInformation("{@LogCode} | UserId: {UserId}", UserLogs.DeleteUser_Success, request.Id);

            return Result<Unit>.Success(Unit.Value);
        }
    }
}
