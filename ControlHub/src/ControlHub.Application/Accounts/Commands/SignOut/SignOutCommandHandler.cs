using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using ControlHub.Application.Common.Persistence;
using ControlHub.Application.Tokens.Interfaces;
using ControlHub.Application.Tokens.Interfaces.Repositories;
using ControlHub.SharedKernel.Accounts;
using ControlHub.SharedKernel.Results;
using ControlHub.SharedKernel.Tokens;
using MediatR;
using Microsoft.Extensions.Logging;

namespace ControlHub.Application.Accounts.Commands.SignOut
{
    public class SignOutCommandHandler : IRequestHandler<SignOutCommand, Result>
    {
        private readonly ITokenRepository _tokenRepository;
        private readonly ITokenQueries _tokenQueries;
        private readonly ITokenVerifier _tokenVerifier;
        private readonly IUnitOfWork _uow;
        private readonly ILogger<SignOutCommandHandler> _logger;

        public SignOutCommandHandler(
            ITokenRepository tokenRepository,
            ITokenQueries tokenQueries,
            ITokenVerifier tokenVerifier,
            IUnitOfWork uow,
            ILogger<SignOutCommandHandler> logger)
        {
            _tokenRepository = tokenRepository;
            _tokenQueries = tokenQueries;
            _tokenVerifier = tokenVerifier;
            _uow = uow;
            _logger = logger;
        }

        public async Task<Result> Handle(SignOutCommand request, CancellationToken cancellationToken)
        {
            _logger.LogInformation("{@LogCode} | AccessToken: {AccessToken}",
                AccountLogs.SignOut_Started,
                request.accessToken[..Math.Min(10, request.accessToken.Length)]); // log 1 ph?n token d? tránh l? full

            var claim = _tokenVerifier.Verify(request.accessToken);
            if (claim == null)
            {
                _logger.LogWarning("{@LogCode} | Reason: {Reason}",
                    AccountLogs.SignOut_InvalidToken,
                    "invalid access token verification");

                return Result.Failure(TokenErrors.TokenInvalid);
            }

            var refreshToken = await _tokenQueries.GetByValueAsync(request.refreshToken, cancellationToken);

            if (refreshToken == null)
            {
                _logger.LogWarning("{@LogCode} | Reason: {Reason}",
                    AccountLogs.SignOut_TokenNotFound,
                     AccountLogs.SignOut_TokenNotFound.Message);

                return Result.Failure(TokenErrors.TokenNotFound);
            }

            var accIdString = claim.Claims.FirstOrDefault(c => c.Type == JwtRegisteredClaimNames.Sub)?.Value
                            ?? claim.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier)?.Value;
            if (!Guid.TryParse(accIdString, out var accId))
            {
                _logger.LogWarning("{@LogCode} | RawId: {RawId}",
                    AccountLogs.SignOut_InvalidAccountId,
                    accIdString);
                foreach (var c in claim.Claims)
                {
                    _logger.LogInformation("Claim: {Type} = {Value}", c.Type, c.Value);
                }

                return Result.Failure(TokenErrors.TokenInvalid);
            }

            var revokeRefreshResult = refreshToken.Revoke();

            if (revokeRefreshResult.IsFailure)
            {
                _logger.LogWarning("{@LogCode} | AccountId: {AccountId}",
                    AccountLogs.SignOut_TokenAlreadyRevoked,
                    accId);

                return Result.Failure(TokenErrors.TokenAlreadyRevoked);
            }

            await _uow.CommitAsync(cancellationToken);

            _logger.LogInformation("{@LogCode} | AccountId: {AccountId}",
                AccountLogs.SignOut_Success,
                accId);

            return Result.Success();
        }
    }
}
