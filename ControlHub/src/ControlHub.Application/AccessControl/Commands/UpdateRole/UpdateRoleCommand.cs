using ControlHub.SharedKernel.Results;
using MediatR;

namespace ControlHub.Application.AccessControl.Commands.UpdateRole
{
    public record UpdateRoleCommand(
        Guid Id,
        string Name,
        string Description
    ) : IRequest<Result<Unit>>;
}
