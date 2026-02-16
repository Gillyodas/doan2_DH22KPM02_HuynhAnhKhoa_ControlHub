using ControlHub.SharedKernel.Results;
using MediatR;

namespace ControlHub.Application.Accounts.Commands.RefreshAccessToken
{
    public sealed record RefreshAccessTokenCommand(string Value, Guid accId, string accessValue) : IRequest<Result<RefreshAccessTokenResponse>>;
}
