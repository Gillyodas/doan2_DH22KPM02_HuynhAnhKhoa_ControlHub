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
        private readonly ITokenCommands _tokenCommands;
        private readonly IUnitOfWork _uow;
        private readonly IAccountQueries _accountQueries;
        private readonly ITokenFactory _tokenFactory;
        private readonly IRefreshTokenGenerator _refreshTokenGenerator;
        private readonly ILogger<RefreshAccessTokenCommandHandler> _logger;
        public RefreshAccessTokenCommandHandler(
            ITokenQueries tokenQueries,
            IAccessTokenGenerator accessTokenGenerator,
            ITokenCommands tokenCommands,
            IUnitOfWork uow,
            IAccountQueries accountQueries,
            ITokenFactory tokenFactory,
            IRefreshTokenGenerator refreshTokenGenerator,
            ILogger<RefreshAccessTokenCommandHandler> logger
            )
        {
            _tokenQueries = tokenQueries;
            _accessTokenGenerator = accessTokenGenerator;
            _tokenCommands = tokenCommands;
            _uow = uow;
            _accountQueries = accountQueries;
            _tokenFactory = tokenFactory;
            _refreshTokenGenerator = refreshTokenGenerator;
            _logger = logger;
        }
        public async Task<Result<RefreshAccessTokenResponse>> Handle(RefreshAccessTokenCommand request, CancellationToken cancellationToken)
        {
            _logger.LogInformation("{Code}: {Message} for RefreshToken {TokenValue}",
                TokenLogs.Refresh_Started.Code,
                TokenLogs.Refresh_Started.Message,
                request.Value);

            var refreshToken = await _tokenQueries.GetByValueAsync(request.Value, cancellationToken);
            if (refreshToken == null)
            {
                _logger.LogWarning("{Code}: {Message} for Token {TokenValue}",
                    TokenLogs.Refresh_NotFound.Code,
                    TokenLogs.Refresh_NotFound.Message,
                    request.Value);
                return Result<RefreshAccessTokenResponse>.Failure(TokenErrors.TokenNotFound);
            }

            var acc = await _accountQueries.GetWithoutUserByIdAsync(request.accId, cancellationToken);
            if (acc == null)
            {
                _logger.LogWarning("{Code}: {Message} for AccountId {AccountId}",
                    TokenLogs.Refresh_AccountNotFound.Code,
                    TokenLogs.Refresh_AccountNotFound.Message,
                    request.accId);
                return Result<RefreshAccessTokenResponse>.Failure(AccountErrors.AccountNotFound);
            }

            if (refreshToken.AccountId != acc.Id)
            {
                _logger.LogWarning("{Code}: {Message} for AccountId {AccountId}, TokenAccountId {TokenAccountId}",
                    TokenLogs.Refresh_TokenMismatch.Code,
                    TokenLogs.Refresh_TokenMismatch.Message,
                    acc.Id,
                    refreshToken.AccountId);
                return Result<RefreshAccessTokenResponse>.Failure(TokenErrors.TokenInvalid);
            }

            if (refreshToken.ExpiredAt <= DateTime.UtcNow || refreshToken.IsUsed)
            {
                _logger.LogWarning("{Code}: {Message} for RefreshToken {TokenValue}, ExpiredAt {ExpiredAt}, IsUsed {IsUsed}",
                    TokenLogs.Refresh_TokenInvalid.Code,
                    TokenLogs.Refresh_TokenInvalid.Message,
                    request.Value,
                    refreshToken.ExpiredAt,
                    refreshToken.IsUsed);
                return Result<RefreshAccessTokenResponse>.Failure(TokenErrors.TokenInvalid);
            }

            _logger.LogInformation("{Code}: {Message} for AccountId {AccountId}",
                TokenLogs.Refresh_Valid.Code,
                TokenLogs.Refresh_Valid.Message,
                acc.Id);

            IEnumerable<string> roles = new[]
            {
                "SuperAdmin",
                "Admin",
                "Manager",
                "Auditor",
                "User"
            };

            var accessTokenValue = _accessTokenGenerator.Generate(acc.Id.ToString(), acc.Identifiers.First().ToString(), roles);
            var newAccessToken = _tokenFactory.Create(acc.Id, accessTokenValue, TokenType.AccessToken);
            await _tokenCommands.AddAsync(newAccessToken, cancellationToken);

            var oldAccessToken = await _tokenQueries.GetByValueAsync(request.accessValue, cancellationToken);
            if (oldAccessToken != null)
            {
                if (oldAccessToken.AccountId != acc.Id)
                {
                    _logger.LogWarning("{Code}: {Message} Old access token mismatch for Account {AccountId}",
                        TokenLogs.Refresh_AccessMismatch.Code,
                        TokenLogs.Refresh_AccessMismatch.Message,
                        acc.Id);
                    return Result<RefreshAccessTokenResponse>.Failure(TokenErrors.TokenInvalid);
                }

                oldAccessToken.MarkAsUsed();
                await _tokenCommands.UpdateAsync(oldAccessToken, cancellationToken);
            }

            refreshToken.MarkAsUsed();
            await _tokenCommands.UpdateAsync(refreshToken, cancellationToken);

            var newRefreshToken = _tokenFactory.Create(acc.Id, _refreshTokenGenerator.Generate(), TokenType.RefreshToken);
            await _tokenCommands.AddAsync(newRefreshToken, cancellationToken);

            await _uow.CommitAsync(cancellationToken);

            _logger.LogInformation("{Code}: {Message} for AccountId {AccountId}. NewAccess: {NewAccess}, NewRefresh: {NewRefresh}",
                TokenLogs.Refresh_Success.Code,
                TokenLogs.Refresh_Success.Message,
                acc.Id,
                accessTokenValue,
                newRefreshToken.Value);

            var result = new RefreshAccessTokenResponse(accessTokenValue, newRefreshToken.Value);
            return Result<RefreshAccessTokenResponse>.Success(result);
        }
    }
}
