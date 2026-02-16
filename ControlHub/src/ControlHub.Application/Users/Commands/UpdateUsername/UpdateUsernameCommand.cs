using ControlHub.SharedKernel.Results;
using MediatR;

namespace ControlHub.Application.Users.Commands.UpdateUsername
{
    public sealed record UpdateUsernameCommand(Guid id, string username) : IRequest<Result<string>>;
}
