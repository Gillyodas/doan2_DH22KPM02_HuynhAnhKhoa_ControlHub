using ControlHub.Application.Accounts.DTOs;
using ControlHub.SharedKernel.Results;
using MediatR;

namespace ControlHub.Application.Accounts.Queries.GetIdentifierConfigs
{
    public record GetIdentifierConfigsQuery : IRequest<Result<List<IdentifierConfigDto>>>;
}
