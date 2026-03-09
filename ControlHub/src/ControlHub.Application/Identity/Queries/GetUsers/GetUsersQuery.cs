using ControlHub.Application.Identity.DTOs;
using ControlHub.SharedKernel.Common;
using ControlHub.SharedKernel.Results;
using MediatR;

namespace ControlHub.Application.Identity.Queries.GetUsers
{
    public record GetUsersQuery(int Page, int PageSize, string? SearchTerm) : IRequest<Result<PaginatedResult<UserDto>>>;
}
