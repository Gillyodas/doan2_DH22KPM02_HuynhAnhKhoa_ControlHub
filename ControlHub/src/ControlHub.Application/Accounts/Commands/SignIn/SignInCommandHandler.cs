using ControlHub.Application.Accounts.DTOs;
using ControlHub.Application.Accounts.Interfaces.Repositories;
using ControlHub.Application.Common.Persistence;
using ControlHub.Application.Tokens.Interfaces;
using ControlHub.Application.Tokens.Interfaces.Generate;
using ControlHub.Application.Tokens.Interfaces.Repositories;
using ControlHub.Domain.Accounts.Identifiers.Interfaces;
using ControlHub.Domain.Accounts.Interfaces.Security;
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
        private readonly IIdentifierValidatorFactory _identifierValidatorFactory;
        private readonly IPasswordHasher _passwordHasher;
        private readonly IAccessTokenGenerator _accessTokenGenerator;
        private readonly IRefreshTokenGenerator _refreshTokenGenerator;
        private readonly ITokenFactory _tokenFactory;
        private readonly ITokenCommands _tokenCommands;
        private readonly IUnitOfWork _uow;

        public SignInCommandHandler(
            ILogger<SignInCommandHandler> logger,
            IAccountQueries accountQueries,
            IIdentifierValidatorFactory identifierValidatorFactory,
            IPasswordHasher passwordHasher,
            IAccessTokenGenerator accessTokenGenerator,
            IRefreshTokenGenerator refreshTokenGenerator,
            ITokenFactory tokenFactory,
            ITokenCommands tokenCommands,
            IUnitOfWork uow)
        {
            _logger = logger;
            _accountQueries = accountQueries;
            _identifierValidatorFactory = identifierValidatorFactory;
            _passwordHasher = passwordHasher;
            _accessTokenGenerator = accessTokenGenerator;
            _refreshTokenGenerator = refreshTokenGenerator;
            _tokenFactory = tokenFactory;
            _tokenCommands = tokenCommands;
            _uow = uow;
        }

        public async Task<Result<SignInDTO>> Handle(SignInCommand request, CancellationToken cancellationToken)
        {
            _logger.LogInformation("{Code}: {Message} for Identifier {Value}",
                AccountLogs.SignIn_Started.Code,
                AccountLogs.SignIn_Started.Message,
                request.Value);

            var validator = _identifierValidatorFactory.Get(request.Type);
            if (validator == null)
            {
                _logger.LogWarning("{Code}: {Message} for Identifier {Value}",
                    AccountLogs.SignIn_InvalidIdentifier.Code,
                    AccountLogs.SignIn_InvalidIdentifier.Message,
                    request.Value);
                return Result<SignInDTO>.Failure(AccountErrors.UnsupportedIdentifierType);
            }

            var (isValid, normalized, error) = validator.ValidateAndNormalize(request.Value);
            if (!isValid)
            {
                _logger.LogWarning("{Code}: {Message} for Identifier {Ident}. Error: {Error}",
                    AccountLogs.SignIn_InvalidIdentifier.Code,
                    AccountLogs.SignIn_InvalidIdentifier.Message,
                    request.Value, error);
                return Result<SignInDTO>.Failure(error);
            }

            var account = await _accountQueries.GetByIdentifierAsync(request.Type, normalized, cancellationToken);
            if (account is null)
            {
                _logger.LogWarning("{Code}: {Message} for Identifier {Ident}",
                    AccountLogs.SignIn_AccountNotFound.Code,
                    AccountLogs.SignIn_AccountNotFound.Message,
                    request.Value);
                return Result<SignInDTO>.Failure(AccountErrors.InvalidCredentials);
            }

            var isPasswordValid = _passwordHasher.Verify(request.Password, account.Password);
            if (!isPasswordValid)
            {
                _logger.LogWarning("{Code}: {Message} for AccountId {AccountId}",
                    AccountLogs.SignIn_InvalidPassword.Code,
                    AccountLogs.SignIn_InvalidPassword.Message,
                    account.Id);
                return Result<SignInDTO>.Failure(AccountErrors.InvalidCredentials);
            }

            IEnumerable<string> roles = new[]
            {
                "User"
            };

            if (account.Identifiers == null || !account.Identifiers.Any())
            {
                _logger.LogWarning("{Code}: {Message} for AccountId {AccountId}",
                    AccountLogs.SignIn_InvalidIdentifier.Code,
                    AccountLogs.SignIn_InvalidIdentifier.Message,
                    account.Id);

                return Result<SignInDTO>.Failure(AccountErrors.InvalidCredentials);
            }

            var accessTokenValue = _accessTokenGenerator.Generate(
                account.Id.ToString(),
                account.Identifiers.First().ToString(),
                roles);

            var refreshTokenValue = _refreshTokenGenerator.Generate();

            if (string.IsNullOrWhiteSpace(accessTokenValue) || string.IsNullOrWhiteSpace(refreshTokenValue))
            {
                _logger.LogError("{Code}: {Message} during token generation for AccountId {AccountId}",
                    TokenLogs.Refresh_TokenInvalid.Code,
                    "Failed to generate tokens",
                    account.Id);
                return Result<SignInDTO>.Failure(TokenErrors.TokenGenerationFailed);
            }

            var accessToken = _tokenFactory.Create(account.Id, accessTokenValue, TokenType.AccessToken);
            var refreshToken = _tokenFactory.Create(account.Id, refreshTokenValue, TokenType.RefreshToken);

            await _tokenCommands.AddAsync(accessToken, cancellationToken);
            await _tokenCommands.AddAsync(refreshToken, cancellationToken);
            await _uow.CommitAsync(cancellationToken);

            _logger.LogInformation("{Code}: {Message} for AccountId {AccountId}",
                AccountLogs.SignIn_Success.Code,
                AccountLogs.SignIn_Success.Message,
                account.Id);

            var dto = new SignInDTO(
                account.Id,
                account.User.Match(some: u => u.Username, none: () => "No name"),
                accessTokenValue,
                refreshTokenValue);

            return Result<SignInDTO>.Success(dto);
        }
    }
}