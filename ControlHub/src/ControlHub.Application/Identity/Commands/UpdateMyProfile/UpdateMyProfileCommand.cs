using ControlHub.SharedKernel.Results;
using MediatR;

namespace ControlHub.Application.Identity.Commands.UpdateMyProfile
{
    public sealed record UpdateMyProfileCommand(string? FirstName, string? LastName, string? PhoneNumber) : IRequest<Result>;
}
