using ControlHub.SharedKernel.Results;
using MediatR;

namespace ControlHub.Application.Identity.Commands.AssignRole
{
    public record AssignRoleToUserCommand(Guid UserId, Guid RoleId) : IRequest<Result<Unit>>;
}
