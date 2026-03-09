using ControlHub.Application.Identity.DTOs;
using ControlHub.SharedKernel.Results;
using MediatR;

namespace ControlHub.Application.Identity.Commands.UpdateIdentifierConfig
{
    public record UpdateIdentifierConfigCommand(
        Guid Id,
        string Name,
        string Description,
        List<ValidationRuleDto> Rules
    ) : IRequest<Result>;
}
