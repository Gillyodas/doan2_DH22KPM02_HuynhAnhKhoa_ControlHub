using System.Collections.Generic;
using ControlHub.Application.Accounts.DTOs;
using ControlHub.SharedKernel.Results;
using MediatR;

namespace ControlHub.Application.Accounts.Queries.GetAdminAccounts
{
    public record GetAdminAccountsQuery : IRequest<Result<List<AccountDto>>>;
}
