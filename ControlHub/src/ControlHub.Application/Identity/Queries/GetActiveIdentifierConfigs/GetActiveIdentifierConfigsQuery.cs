using ControlHub.Application.Identity.DTOs;
using ControlHub.SharedKernel.Results;
using MediatR;

namespace ControlHub.Application.Identity.Queries.GetActiveIdentifierConfigs
{
    public record GetActiveIdentifierConfigsQuery(bool IncludeDeactivated = false) : IRequest<Result<List<IdentifierConfigDto>>>;
}
