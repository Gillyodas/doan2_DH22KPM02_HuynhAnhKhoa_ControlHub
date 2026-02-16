using ControlHub.Application.Accounts.Interfaces.Repositories;
using ControlHub.Application.Common.Persistence;
using ControlHub.Application.Users.Interfaces.Repositories;
using ControlHub.SharedKernel.Results;
using ControlHub.SharedKernel.Users;
using MediatR;
using Microsoft.Extensions.Logging;

namespace ControlHub.Application.Users.Commands.UpdateUsername
{
    public class UpdateUsernameCommandHandler : IRequestHandler<UpdateUsernameCommand, Result<string>>
    {
        private readonly ILogger<UpdateUsernameCommandHandler> _logger;
        private readonly IUnitOfWork _uow;
        private IUserRepository _userRepository;

        public UpdateUsernameCommandHandler(ILogger<UpdateUsernameCommandHandler> logger, IUserRepository userRepo, IUnitOfWork uow)
        {
            _logger = logger;
            _uow = uow;
            _userRepository = userRepo;
        }

        public async Task<Result<string>> Handle(UpdateUsernameCommand request, CancellationToken cancellationToken)
        {
            _logger.LogInformation("{@LogCode} | AccountId: {AccountId} | NewUsername: {NewUsername}",
                UserLogs.UpdateUsername_Started,
                request.id,
                request.username);

            var user = await _userRepository.GetByAccountId(request.id, cancellationToken);

            if (user is null)
            {
                _logger.LogWarning("{@LogCode} | AccountId: {AccountId}",
                    UserLogs.UpdateUsername_NotFound,
                    request.id);
                return Result<string>.Failure(UserErrors.NotFound);
            }

            var updateResult = user.UpdateUsername(request.username);

            if (!updateResult.IsSuccess)
            {
                _logger.LogWarning("{@LogCode} | Error: {Error}",
                    UserLogs.UpdateUsername_Failed,
                    updateResult.Error.Code);
                return Result<string>.Failure(updateResult.Error);
            }
            

            await _uow.CommitAsync(cancellationToken);

            _logger.LogInformation("{@LogCode} | AccountId: {AccountId} | Username: {Username}",
                UserLogs.UpdateUsername_Success,
                request.id,
                user.Username);

            return Result<string>.Success(user.Username);
        }
    }
}
