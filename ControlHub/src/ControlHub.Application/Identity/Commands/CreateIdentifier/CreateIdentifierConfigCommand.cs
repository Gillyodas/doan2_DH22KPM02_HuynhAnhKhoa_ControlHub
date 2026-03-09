using ControlHub.Application.Identity.DTOs;
using ControlHub.SharedKernel.Results;
using MediatR;

namespace ControlHub.Application.Identity.Commands.CreateIdentifier
{
    public record CreateIdentifierConfigCommand(
    string Name,
    string Description,
    List<ValidationRuleDto> Rules
) : IRequest<Result<Guid>>;
}
