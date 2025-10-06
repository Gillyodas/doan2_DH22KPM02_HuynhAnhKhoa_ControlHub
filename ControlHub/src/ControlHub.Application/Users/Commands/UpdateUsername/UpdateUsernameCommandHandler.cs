using ControlHub.Application.Accounts.Interfaces.Repositories;
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
        private readonly IUserCommands _userCommands;
        private readonly IUserQueries _userQueries;
        private readonly IAccountQueries _accountQueries;

        public UpdateUsernameCommandHandler(ILogger<UpdateUsernameCommandHandler> logger, IUserCommands userCommands, IUserQueries userQueries, IAccountQueries accountQueries)
        {
            _logger = logger;
            _userCommands = userCommands;
            _userQueries = userQueries;
            _accountQueries = accountQueries;
        }

        public async Task<Result<string>> Handle(UpdateUsernameCommand request, CancellationToken cancellationToken)
        {
            var user = await _accountQueries.GetUserById(request.id, cancellationToken);

            if (user is null)
                return Result<string>.Failure(UserErrors.NotFound);

            var updateResult = user.UpdateUsername(request.username);

            if (!updateResult.IsSuccess)
                return Result<string>.Failure(updateResult.Error);

            await _userCommands.SaveAsync(user, cancellationToken);

            return Result<string>.Success(user.Username);
        }
    }
}
