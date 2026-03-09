using ControlHub.Application.Identity.DTOs;
using ControlHub.SharedKernel.Results;
using MediatR;

namespace ControlHub.Application.Identity.Queries.GetUserById
{
    public record GetUserByIdQuery(Guid Id) : IRequest<Result<UserDto>>;
}
