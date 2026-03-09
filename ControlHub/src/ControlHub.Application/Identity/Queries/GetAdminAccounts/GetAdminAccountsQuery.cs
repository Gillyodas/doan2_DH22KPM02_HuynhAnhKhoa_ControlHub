using ControlHub.Application.Identity.DTOs;
using ControlHub.SharedKernel.Results;
using MediatR;

namespace ControlHub.Application.Identity.Queries.GetAdminAccounts
{
    public record GetAdminAccountsQuery : IRequest<Result<List<AccountDto>>>;
}
