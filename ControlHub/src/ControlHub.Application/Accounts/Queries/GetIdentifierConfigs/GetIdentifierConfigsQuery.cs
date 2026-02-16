using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ControlHub.Application.Accounts.DTOs;
using ControlHub.SharedKernel.Results;
using MediatR;

namespace ControlHub.Application.Accounts.Queries.GetIdentifierConfigs
{
    public record GetIdentifierConfigsQuery : IRequest<Result<List<IdentifierConfigDto>>>;
}
