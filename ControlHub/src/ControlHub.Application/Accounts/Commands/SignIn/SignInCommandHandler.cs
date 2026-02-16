using System.IO;
using ControlHub.Application.Accounts.DTOs;
using ControlHub.Application.Accounts.Interfaces.Repositories;
using ControlHub.Application.Common.Events;
using ControlHub.Application.Common.Persistence;
using ControlHub.Application.Tokens.Interfaces;
using ControlHub.Application.Tokens.Interfaces.Generate;
using ControlHub.Application.Tokens.Interfaces.Repositories;
using ControlHub.Domain.Identity.Enums;
using ControlHub.Domain.Identity.Identifiers.Services;
using ControlHub.Domain.Identity.Security;
using ControlHub.Domain.Tokens.Enums;
using ControlHub.SharedKernel.Accounts;
using ControlHub.SharedKernel.Results;
using ControlHub.SharedKernel.Tokens;
using MediatR;
using Microsoft.Extensions.Logging;

namespace ControlHub.Application.Accounts.Commands.SignIn
{
    public class SignInCommandHandler : IRequestHandler<SignInCommand, Result<SignInDTO>>
    {
        private readonly ILogger<SignInCommandHandler> _logger;
        private readonly IAccountQueries _accountQueries;
        private readonly IdentifierFactory _identifierFactory;
        private readonly IPasswordHasher _passwordHasher;
        private readonly IAccessTokenGenerator _accessTokenGenerator;
        private readonly IRefreshTokenGenerator _refreshTokenGenerator;
        private readonly ITokenFactory _tokenFactory;
        private readonly ITokenRepository _tokenRepository;
        private readonly IUnitOfWork _uow;
        private readonly IPublisher _publisher;

        public SignInCommandHandler(
            ILogger<SignInCommandHandler> logger,
            IAccountQueries accountQueries,
            IdentifierFactory identifierFactory,
            IPasswordHasher passwordHasher,
            IAccessTokenGenerator accessTokenGenerator,
            IRefreshTokenGenerator refreshTokenGenerator,
            ITokenFactory tokenFactory,
            ITokenRepository tokenRepository,
            IUnitOfWork uow,
            IPublisher publisher)
        {
            _logger = logger;
            _accountQueries = accountQueries;
            _identifierFactory = identifierFactory;
            _passwordHasher = passwordHasher;
            _accessTokenGenerator = accessTokenGenerator;
            _refreshTokenGenerator = refreshTokenGenerator;
            _tokenFactory = tokenFactory;
            _tokenRepository = tokenRepository;
            _uow = uow;
            _publisher = publisher;
        }

        public async Task<Result<SignInDTO>> Handle(SignInCommand request, CancellationToken cancellationToken)
        {
            _logger.LogInformation("{@LogCode} | Ident: {Value}",
                AccountLogs.SignIn_Started,
                request.Value);

            var result = await _identifierFactory.CreateAsync(request.Type, request.Value, request.IdentifierConfigId, cancellationToken);
            if (result.IsFailure)
            {
                _ = PublishLoginEvent(request, false, "Account not found");
                _logger.LogWarning("{@LogCode} | Ident: {Ident} | Error: {Error}",
                    AccountLogs.SignIn_InvalidIdentifier,
                    request.Value, result.Error);
                return Result<SignInDTO>.Failure(result.Error);
            }

            var account = await _accountQueries.GetByIdentifierAsync(request.Type, result.Value.NormalizedValue, cancellationToken);
            if (account is null || account.IsDeleted == true || account.IsActive == false)
            {
                _ = PublishLoginEvent(request, false, "Account not found or inactive");
                _logger.LogWarning("{@LogCode} | Ident: {Ident}",
                    AccountLogs.SignIn_AccountNotFound,
                    request.Value);
                return Result<SignInDTO>.Failure(AccountErrors.InvalidCredentials);
            }

            var isPasswordValid = _passwordHasher.Verify(request.Password, account.Password);
            if (!isPasswordValid)
            {
                _ = PublishLoginEvent(request, false, "Invalid password");
                _logger.LogWarning("{@LogCode} | AccountId: {AccountId}",
                    AccountLogs.SignIn_InvalidPassword,
                    account.Id);
                return Result<SignInDTO>.Failure(AccountErrors.InvalidCredentials);
            }

            var roleId = await _accountQueries.GetRoleIdByAccIdAsync(account.Id, cancellationToken);

            if (account.Identifiers == null || !account.Identifiers.Any())
            {
                _ = PublishLoginEvent(request, false, "No identifiers found for account");
                _logger.LogWarning("{@LogCode} | AccountId: {AccountId}",
                    AccountLogs.SignIn_InvalidIdentifier,
                    account.Id);

                return Result<SignInDTO>.Failure(AccountErrors.InvalidCredentials);
            }

            var accessTokenValue = _accessTokenGenerator.Generate(
                account.Id.ToString(),
                account.Identifiers.First().ToString(),
                roleId.ToString());

            var refreshTokenValue = _refreshTokenGenerator.Generate();

            if (string.IsNullOrWhiteSpace(accessTokenValue) || string.IsNullOrWhiteSpace(refreshTokenValue))
            {
                _logger.LogError("{@LogCode} | AccountId: {AccountId} | Reason: {Reason}",
                    TokenLogs.Refresh_TokenInvalid,
                    "Failed to generate tokens",
                    account.Id);
                return Result<SignInDTO>.Failure(TokenErrors.TokenGenerationFailed);
            }

            var accessToken = _tokenFactory.Create(account.Id, accessTokenValue, TokenType.AccessToken);
            var refreshToken = _tokenFactory.Create(account.Id, refreshTokenValue, TokenType.RefreshToken);

            await _tokenRepository.AddAsync(accessToken, cancellationToken);
            await _tokenRepository.AddAsync(refreshToken, cancellationToken);
            await _uow.CommitAsync(cancellationToken);

            _ = PublishLoginEvent(request, true, null);
            _logger.LogInformation("{@LogCode} | AccountId: {AccountId}",
                AccountLogs.SignIn_Success,
                account.Id);

            var dto = new SignInDTO(
            account.Id,
            account.User?.Username ?? "No name",
            accessTokenValue,
            refreshTokenValue);

            return Result<SignInDTO>.Success(dto);
        }

        private Task PublishLoginEvent(SignInCommand req, bool success, string? reason)
        {
            return _publisher.Publish(new LoginAttemptedEvent
            {
                IsSuccess = success,
                IdentifierType = req.Type.ToString(),
                MaskedIdentifier = MaskIdentifier(req.Value),
                FailureReason = reason
            });
        }

        private static string MaskIdentifier(string value)
        {
            if (string.IsNullOrEmpty(value) || value.Length < 4) return "***";
            if (value.Contains('@'))
            {
                var parts = value.Split('@');
                return $"{parts[0][0]}***@{(parts.Length > 1 ? parts[1] : "")}";
            }
            return $"{value[..3]}***";
        }
    }
}
