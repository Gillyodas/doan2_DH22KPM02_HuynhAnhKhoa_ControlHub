using ControlHub.SharedKernel.Results;
using MediatR;

namespace ControlHub.Application.Identity.Commands.RefreshAccessToken
{
    public sealed record RefreshAccessTokenCommand(string Value, Guid accId, string accessValue) : IRequest<Result<RefreshAccessTokenResponse>>;
}
