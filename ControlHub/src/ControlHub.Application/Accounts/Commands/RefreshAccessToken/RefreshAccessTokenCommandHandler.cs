using ControlHub.Application.Accounts.Interfaces.Repositories;
using ControlHub.Application.Common.Persistence;
using ControlHub.Application.Tokens.Interfaces;
using ControlHub.Application.Tokens.Interfaces.Generate;
using ControlHub.Application.Tokens.Interfaces.Repositories;
using ControlHub.Domain.Tokens.Enums;
using ControlHub.SharedKernel.Accounts;
using ControlHub.SharedKernel.Results;
using ControlHub.SharedKernel.Tokens;
using MediatR;
using Microsoft.Extensions.Logging;

namespace ControlHub.Application.Accounts.Commands.RefreshAccessToken
{
    public class RefreshAccessTokenCommandHandler : IRequestHandler<RefreshAccessTokenCommand, Result<RefreshAccessTokenResponse>>
    {
        private readonly ITokenQueries _tokenQueries;
        private readonly IAccessTokenGenerator _accessTokenGenerator;
        private readonly ITokenRepository _tokenRepository;
        private readonly IUnitOfWork _uow;
        private readonly IAccountQueries _accountQueries;
        private readonly ITokenFactory _tokenFactory;
        private readonly IRefreshTokenGenerator _refreshTokenGenerator;
        private readonly ILogger<RefreshAccessTokenCommandHandler> _logger;
        public RefreshAccessTokenCommandHandler(
            ITokenQueries tokenQueries,
            IAccessTokenGenerator accessTokenGenerator,
            ITokenRepository tokenRepository,
            IUnitOfWork uow,
            IAccountQueries accountQueries,
            ITokenFactory tokenFactory,
            IRefreshTokenGenerator refreshTokenGenerator,
            ILogger<RefreshAccessTokenCommandHandler> logger
            )
        {
            _tokenQueries = tokenQueries;
            _accessTokenGenerator = accessTokenGenerator;
            _tokenRepository = tokenRepository;
            _uow = uow;
            _accountQueries = accountQueries;
            _tokenFactory = tokenFactory;
            _refreshTokenGenerator = refreshTokenGenerator;
            _logger = logger;
        }
        public async Task<Result<RefreshAccessTokenResponse>> Handle(RefreshAccessTokenCommand request, CancellationToken cancellationToken)
        {
            _logger.LogInformation("{@LogCode} | RefreshToken: {TokenValue}",
                TokenLogs.Refresh_Started,
                request.Value);

            var refreshToken = await _tokenQueries.GetByValueAsync(request.Value, cancellationToken);
            if (refreshToken == null)
            {
                _logger.LogWarning("{@LogCode} | Token: {TokenValue}",
                    TokenLogs.Refresh_NotFound,
                    request.Value);
                return Result<RefreshAccessTokenResponse>.Failure(TokenErrors.TokenNotFound);
            }

            var acc = await _accountQueries.GetWithoutUserByIdAsync(request.accId, cancellationToken);
            if (acc == null)
            {
                _logger.LogWarning("{@LogCode} | AccountId: {AccountId}",
                    TokenLogs.Refresh_AccountNotFound,
                    request.accId);
                return Result<RefreshAccessTokenResponse>.Failure(AccountErrors.AccountNotFound);
            }

            if (refreshToken.AccountId != acc.Id)
            {
                _logger.LogWarning("{@LogCode} | AccountId: {AccountId} | TokenAccountId: {TokenAccountId}",
                    TokenLogs.Refresh_TokenMismatch,
                    acc.Id,
                    refreshToken.AccountId);
                return Result<RefreshAccessTokenResponse>.Failure(TokenErrors.TokenInvalid);
            }

            if (refreshToken.ExpiredAt <= DateTime.UtcNow || refreshToken.IsUsed)
            {
                _logger.LogWarning("{@LogCode} | RefreshToken: {TokenValue} | ExpiredAt: {ExpiredAt} | IsUsed: {IsUsed}",
                    TokenLogs.Refresh_TokenInvalid,
                    request.Value,
                    refreshToken.ExpiredAt,
                    refreshToken.IsUsed);
                return Result<RefreshAccessTokenResponse>.Failure(TokenErrors.TokenInvalid);
            }

            _logger.LogInformation("{@LogCode} | AccountId: {AccountId}",
                TokenLogs.Refresh_Valid,
                acc.Id);

            var roleId = await _accountQueries.GetRoleIdByAccIdAsync(acc.Id, cancellationToken);

            var accessTokenValue = _accessTokenGenerator.Generate(acc.Id.ToString(), acc.Identifiers.First().ToString(), roleId.ToString());
            var newAccessToken = _tokenFactory.Create(acc.Id, accessTokenValue, TokenType.AccessToken);
            await _tokenRepository.AddAsync(newAccessToken, cancellationToken);

            var oldAccessToken = await _tokenQueries.GetByValueAsync(request.accessValue, cancellationToken);
            if (oldAccessToken != null)
            {
                if (oldAccessToken.AccountId != acc.Id)
                {
                    _logger.LogWarning("{@LogCode} | AccountId: {AccountId}",
                        TokenLogs.Refresh_AccessMismatch,
                        acc.Id);
                    return Result<RefreshAccessTokenResponse>.Failure(TokenErrors.TokenInvalid);
                }

                oldAccessToken.MarkAsUsed();
                oldAccessToken.Revoke();
            }

            refreshToken.MarkAsUsed();
            refreshToken.Revoke();

            var newRefreshToken = _tokenFactory.Create(acc.Id, _refreshTokenGenerator.Generate(), TokenType.RefreshToken);
            await _tokenRepository.AddAsync(newRefreshToken, cancellationToken);

            await _uow.CommitAsync(cancellationToken);

            _logger.LogInformation("{@LogCode} | AccountId: {AccountId} | NewAccess: {NewAccess} | NewRefresh: {NewRefresh}",
                TokenLogs.Refresh_Success,
                acc.Id,
                accessTokenValue,
                newRefreshToken.Value);

            var result = new RefreshAccessTokenResponse(accessTokenValue, newRefreshToken.Value);
            return Result<RefreshAccessTokenResponse>.Success(result);
        }
    }
}
