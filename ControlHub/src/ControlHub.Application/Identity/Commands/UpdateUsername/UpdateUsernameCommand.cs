using ControlHub.SharedKernel.Results;
using MediatR;

namespace ControlHub.Application.Identity.Commands.UpdateUsername
{
    public sealed record UpdateUsernameCommand(Guid id, string username) : IRequest<Result<string>>;
}
