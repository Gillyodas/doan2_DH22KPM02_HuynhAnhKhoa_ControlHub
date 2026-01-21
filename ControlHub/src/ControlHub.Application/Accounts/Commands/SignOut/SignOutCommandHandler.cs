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
            _logger.LogInformation("{Code}: {Message} for AccessToken {AccessToken}",
                AccountLogs.SignOut_Started.Code,
                AccountLogs.SignOut_Started.Message,
                request.accessToken[..Math.Min(10, request.accessToken.Length)]); // log 1 phần token để tránh lộ full

            var claim = _tokenVerifier.Verify(request.accessToken);
            if (claim == null)
            {
                _logger.LogWarning("{Code}: {Message} - invalid access token",
                    AccountLogs.SignOut_InvalidToken.Code,
                    AccountLogs.SignOut_InvalidToken.Message);

                return Result.Failure(TokenErrors.TokenInvalid);
            }

            var accessToken = await _tokenQueries.GetByValueAsync(request.accessToken, cancellationToken);
            var refreshToken = await _tokenQueries.GetByValueAsync(request.refreshToken, cancellationToken);

            if (accessToken == null || refreshToken == null)
            {
                _logger.LogWarning("{Code}: {Message} - token not found in storage",
                    AccountLogs.SignOut_TokenNotFound.Code,
                    AccountLogs.SignOut_TokenNotFound.Message);

                return Result.Failure(TokenErrors.TokenNotFound);
            }

            var accIdString = claim.Claims.FirstOrDefault(c => c.Type == JwtRegisteredClaimNames.Sub)?.Value
                            ?? claim.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier)?.Value;
            if (!Guid.TryParse(accIdString, out var accId))
            {
                _logger.LogWarning("{Code}: {Message} - invalid AccountId format:{id}",
                    AccountLogs.SignOut_InvalidAccountId.Code,
                    AccountLogs.SignOut_InvalidAccountId.Message,
                    accIdString);
                foreach (var c in claim.Claims)
                {
                    _logger.LogInformation("Claim: {Type} = {Value}", c.Type, c.Value);
                }

                return Result.Failure(TokenErrors.TokenInvalid);
            }

            if (accessToken.AccountId != accId || refreshToken.AccountId != accId)
            {
                _logger.LogWarning("{Code}: {Message} - mismatched AccountId {AccountId}",
                    AccountLogs.SignOut_MismatchedAccount.Code,
                    AccountLogs.SignOut_MismatchedAccount.Message,
                    accId);

                return Result.Failure(TokenErrors.TokenInvalid);
            }

            var revokeAccessResult = accessToken.Revoke();
            var revokeRefreshResult = refreshToken.Revoke();

            if (revokeAccessResult.IsFailure || revokeRefreshResult.IsFailure)
            {
                _logger.LogWarning("{Code}: {Message} for AccountId {AccountId}",
                    AccountLogs.SignOut_TokenAlreadyRevoked.Code,
                    AccountLogs.SignOut_TokenAlreadyRevoked.Message,
                    accId);

                return Result.Failure(TokenErrors.TokenAlreadyRevoked);
            }

            await _uow.CommitAsync(cancellationToken);

            _logger.LogInformation("{Code}: {Message} for AccountId {AccountId}",
                AccountLogs.SignOut_Success.Code,
                AccountLogs.SignOut_Success.Message,
                accId);

            return Result.Success();
        }
    }
}