using ControlHub.Application.Identity.DTOs;
using ControlHub.SharedKernel.Results;
using MediatR;

namespace ControlHub.Application.Identity.Queries.GetIdentifierConfigs
{
    public record GetIdentifierConfigsQuery : IRequest<Result<List<IdentifierConfigDto>>>;
}
