using ControlHub.Application.Users.DTOs;
using ControlHub.Application.Users.Interfaces.Repositories;
using ControlHub.SharedKernel.Common;
using ControlHub.SharedKernel.Results;
using ControlHub.SharedKernel.Users;
using MediatR;
using Microsoft.Extensions.Logging;

namespace ControlHub.Application.Users.Queries.GetUsers
{
    public class GetUsersQueryHandler : IRequestHandler<GetUsersQuery, Result<PaginatedResult<UserDto>>>
    {
        private readonly IUserQueries _userQueries;
        private readonly ILogger<GetUsersQueryHandler> _logger;

        public GetUsersQueryHandler(IUserQueries userQueries, ILogger<GetUsersQueryHandler> logger)
        {
            _userQueries = userQueries;
            _logger = logger;
        }

        public async Task<Result<PaginatedResult<UserDto>>> Handle(GetUsersQuery request, CancellationToken ct)
        {
            _logger.LogInformation("{@LogCode} | Page: {Page} | PageSize: {PageSize}",
                UserLogs.GetUsers_Started, request.Page, request.PageSize);

            var result = await _userQueries.GetPaginatedAsync(request.Page, request.PageSize, request.SearchTerm, ct);

            _logger.LogInformation("{@LogCode} | Count: {Count}", UserLogs.GetUsers_Success, result.Items.Count);

            return Result<PaginatedResult<UserDto>>.Success(result);
        }
    }
}
