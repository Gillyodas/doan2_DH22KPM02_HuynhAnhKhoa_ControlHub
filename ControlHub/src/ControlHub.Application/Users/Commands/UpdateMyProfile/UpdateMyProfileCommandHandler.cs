using ControlHub.Application.Common.Interfaces;
using ControlHub.Application.Common.Persistence;
using ControlHub.Application.Users.Interfaces.Repositories;
using ControlHub.SharedKernel.Results;
using ControlHub.SharedKernel.Users;
using MediatR;
using Microsoft.Extensions.Logging;

namespace ControlHub.Application.Users.Commands.UpdateMyProfile
{
    public class UpdateMyProfileCommandHandler : IRequestHandler<UpdateMyProfileCommand, Result>
    {
        private readonly ICurrentUserService _currentUserService;
        private readonly IUserRepository _userRepository;
        private readonly IUnitOfWork _uow;
        private readonly ILogger<UpdateMyProfileCommandHandler> _logger;

        public UpdateMyProfileCommandHandler(
            ICurrentUserService currentUserService,
            IUserRepository userRepository,
            IUnitOfWork uow,
            ILogger<UpdateMyProfileCommandHandler> logger)
        {
            _currentUserService = currentUserService;
            _userRepository = userRepository;
            _uow = uow;
            _logger = logger;
        }

        public async Task<Result> Handle(UpdateMyProfileCommand request, CancellationToken cancellationToken)
        {
            var userId = _currentUserService.UserId;

            _logger.LogInformation("{@LogCode} | AccountId: {AccountId}", UserLogs.UpdateMyProfile_Started, userId);

            if (userId == Guid.Empty)
            {
                return Result.Failure(UserErrors.NotFound);
            }

            var user = await _userRepository.GetByAccountId(userId, cancellationToken);
            if (user == null)
            {
                return Result.Failure(UserErrors.NotFound);
            }

            user.UpdateProfile(request.FirstName, request.LastName, request.PhoneNumber);

            await _uow.CommitAsync(cancellationToken);

            _logger.LogInformation("{@LogCode} | AccountId: {AccountId}", UserLogs.UpdateMyProfile_Success, userId);

            return Result.Success();
        }
    }
}
