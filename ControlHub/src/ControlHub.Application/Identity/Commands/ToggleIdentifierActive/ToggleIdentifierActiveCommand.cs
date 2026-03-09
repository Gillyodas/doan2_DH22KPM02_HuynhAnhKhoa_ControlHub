using ControlHub.SharedKernel.Results;
using MediatR;

namespace ControlHub.Application.Identity.Commands.ToggleIdentifierActive
{
    public record ToggleIdentifierActiveCommand(Guid Id, bool IsActive) : IRequest<Result>;
}
