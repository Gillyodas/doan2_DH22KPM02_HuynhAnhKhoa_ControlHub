using ControlHub.Application.Accounts.DTOs;
using ControlHub.Domain.Identity.Enums;
using ControlHub.SharedKernel.Results;
using MediatR;

namespace ControlHub.Application.Accounts.Commands.SignIn
{
    public sealed record SignInCommand(string Value, string Password,Guid? IdentifierConfigId = null) : IRequest<Result<SignInDTO>>;
}
