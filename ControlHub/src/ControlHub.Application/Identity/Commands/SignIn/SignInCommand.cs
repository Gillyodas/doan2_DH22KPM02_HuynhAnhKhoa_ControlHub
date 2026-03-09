using ControlHub.Application.Identity.DTOs;
using ControlHub.Domain.Identity.Enums;
using ControlHub.SharedKernel.Results;
using MediatR;

namespace ControlHub.Application.Identity.Commands.SignIn
{
    public sealed record SignInCommand(string Value, string Password) : IRequest<Result<SignInDTO>>;
}
