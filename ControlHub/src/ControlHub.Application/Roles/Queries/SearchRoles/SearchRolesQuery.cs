using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ControlHub.Application.Common.DTOs;
using ControlHub.Domain.Roles;
using ControlHub.SharedKernel.Results;
using MediatR;

namespace ControlHub.Application.Roles.Queries.SearchRoles
{
    public sealed record SearchRolesQuery(int pageIndex, int pageSize, string[] conditions) : IRequest<Result<PagedResult<Role>>>;
}
