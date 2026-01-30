using ControlHub.Application.Common.Interfaces;
using ControlHub.Application.Users.DTOs;
using ControlHub.Application.Users.Interfaces.Repositories;
using ControlHub.SharedKernel.Results;
using ControlHub.SharedKernel.Users;
using MediatR;
using Microsoft.Extensions.Logging;

namespace ControlHub.Application.Users.Queries.GetMyProfile
{
    public sealed record GetMyProfileQuery : IRequest<Result<UserDto>>;

    public class GetMyProfileQueryHandler : IRequestHandler<GetMyProfileQuery, Result<UserDto>>
    {
        private readonly ICurrentUserService _currentUserService;
        private readonly IUserQueries _userQueries;
        private readonly ILogger<GetMyProfileQueryHandler> _logger;

        public GetMyProfileQueryHandler(
            ICurrentUserService currentUserService,
            IUserQueries userQueries,
            ILogger<GetMyProfileQueryHandler> logger)
        {
            _currentUserService = currentUserService;
            _userQueries = userQueries;
            _logger = logger;
        }

        public async Task<Result<UserDto>> Handle(GetMyProfileQuery request, CancellationToken cancellationToken)
        {
            var userId = _currentUserService.UserId;

            _logger.LogInformation("{@LogCode} | AccountId: {AccountId}", UserLogs.GetMyProfile_Started, userId);

            if (userId == Guid.Empty)
            {
                // This typically shouldn't happen if the endpoint is [Authorize], but good to handle.
                return Result<UserDto>.Failure(UserErrors.NotFound);
            }

            var userDto = await _userQueries.GetDtoByAccountId(userId, cancellationToken);

            if (userDto == null)
            {
                _logger.LogWarning("{@LogCode} | AccountId: {AccountId}", UserLogs.GetMyProfile_NotFound, userId);
                return Result<UserDto>.Failure(UserErrors.NotFound);
            }

            _logger.LogInformation("{@LogCode} | AccountId: {AccountId}", UserLogs.GetMyProfile_Success, userId);

            return Result<UserDto>.Success(userDto);
        }
    }
}
