using ControlHub.Application.Accounts.DTOs;
using ControlHub.Domain.Accounts.ValueObjects;
using ControlHub.SharedKernel.Results;
using MediatR;

namespace ControlHub.Application.Accounts.Commands.SignIn
{
    public sealed record SignInCommand(string email, string password) : IRequest<Result<SignInDTO>>;
}
