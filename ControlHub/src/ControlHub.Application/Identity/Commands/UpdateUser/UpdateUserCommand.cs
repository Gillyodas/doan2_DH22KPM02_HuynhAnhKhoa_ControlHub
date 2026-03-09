using ControlHub.Application.Identity.DTOs;
using ControlHub.SharedKernel.Results;
using MediatR;

namespace ControlHub.Application.Identity.Commands.UpdateUser
{
    public record UpdateUserCommand(
        Guid Id,
        string? Email,
        string? FirstName,
        string? LastName,
        string? PhoneNumber,
        bool? IsActive
    ) : IRequest<Result<UserDto>>;
}
