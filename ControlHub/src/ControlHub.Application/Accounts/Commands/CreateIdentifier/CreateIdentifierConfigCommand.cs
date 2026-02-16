using ControlHub.Application.Accounts.DTOs;
using ControlHub.SharedKernel.Results;
using MediatR;

namespace ControlHub.Application.Accounts.Commands.CreateIdentifier
{
    public record CreateIdentifierConfigCommand(
    string Name,
    string Description,
    List<ValidationRuleDto> Rules
) : IRequest<Result<Guid>>;
}
