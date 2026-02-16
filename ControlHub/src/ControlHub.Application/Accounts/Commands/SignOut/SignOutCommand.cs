using ControlHub.SharedKernel.Results;
using MediatR;

namespace ControlHub.Application.Accounts.Commands.SignOut
{
    public sealed record SignOutCommand(string accessToken, string refreshToken) : IRequest<Result>;
}
