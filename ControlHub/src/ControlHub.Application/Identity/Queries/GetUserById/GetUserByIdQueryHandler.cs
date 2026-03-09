using ControlHub.Application.Identity.DTOs;
using ControlHub.Application.Identity.Interfaces.Repositories;
using ControlHub.SharedKernel.Results;
using ControlHub.SharedKernel.Users;
using MediatR;

using Microsoft.Extensions.Logging;

namespace ControlHub.Application.Identity.Queries.GetUserById
{
    public class GetUserByIdQueryHandler : IRequestHandler<GetUserByIdQuery, Result<UserDto>>
    {
        private readonly IUserQueries _userQueries;
        private readonly ILogger<GetUserByIdQueryHandler> _logger;

        public GetUserByIdQueryHandler(IUserQueries userQueries, ILogger<GetUserByIdQueryHandler> logger)
        {
            _userQueries = userQueries;
            _logger = logger;
        }

        public async Task<Result<UserDto>> Handle(GetUserByIdQuery request, CancellationToken ct)
        {
            _logger.LogInformation("{@LogCode} | UserId: {UserId}", UserLogs.GetUserById_Started, request.Id);

            var userDto = await _userQueries.GetByIdAsync(request.Id, ct);

            if (userDto == null)
            {
                _logger.LogWarning("{@LogCode} | UserId: {UserId}", UserLogs.GetUserById_NotFound, request.Id);
                return Result<UserDto>.Failure(UserErrors.NotFound);
            }

            return Result<UserDto>.Success(userDto);
        }
    }
}
