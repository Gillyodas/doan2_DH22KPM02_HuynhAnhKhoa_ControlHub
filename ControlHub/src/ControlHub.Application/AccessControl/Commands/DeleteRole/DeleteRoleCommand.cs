using ControlHub.SharedKernel.Results;
using MediatR;

namespace ControlHub.Application.AccessControl.Commands.DeleteRole
{
    public record DeleteRoleCommand(Guid Id) : IRequest<Result<Unit>>;
}
