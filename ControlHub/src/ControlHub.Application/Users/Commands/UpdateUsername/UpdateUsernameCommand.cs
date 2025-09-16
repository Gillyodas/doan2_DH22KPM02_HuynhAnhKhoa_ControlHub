using MediatR;
using ControlHub.SharedKernel.Results;

namespace ControlHub.Application.Users.Commands.UpdateUsername
{
    public sealed record UpdateUsernameCommand(Guid id, string username) : IRequest<Result<string>>;
}
