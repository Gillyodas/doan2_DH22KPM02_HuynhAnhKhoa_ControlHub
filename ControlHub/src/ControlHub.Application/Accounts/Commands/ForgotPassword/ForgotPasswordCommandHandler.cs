using System.Text.Json;
using ControlHub.Application.Accounts.Interfaces.Repositories;
using ControlHub.Application.Common.Persistence;
using ControlHub.Application.OutBoxs.Repositories;
using ControlHub.Application.Tokens.Interfaces;
using ControlHub.Application.Tokens.Interfaces.Generate;
using ControlHub.Application.Tokens.Interfaces.Repositories;
using ControlHub.Domain.Identity.Identifiers.Services;
using ControlHub.Domain.Outboxs;
using ControlHub.Domain.Tokens.Enums;
using ControlHub.SharedKernel.Accounts;
using ControlHub.SharedKernel.Common.Errors;
using ControlHub.SharedKernel.Common.Logs;
using ControlHub.SharedKernel.Results;
using ControlHub.SharedKernel.Tokens;
using MediatR;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace ControlHub.Application.Accounts.Commands.ForgotPassword
{
    public class ForgotPasswordCommandHandler : IRequestHandler<ForgotPasswordCommand, Result>
    {
        private readonly IPasswordResetTokenGenerator _passwordResetTokenGenerator;
        private readonly IAccountRepository _accountRepository;
        private readonly ILogger<ForgotPasswordCommandHandler> _logger;
        private readonly IdentifierFactory _identifierFactory;
        private readonly IUnitOfWork _uow;
        private readonly ITokenRepository _tokenRepository;
        private readonly ITokenFactory _tokenFactory;
        private readonly IOutboxRepository _outboxCommands;
        private readonly IConfiguration _configuration;

        public ForgotPasswordCommandHandler(
            IPasswordResetTokenGenerator passwordResetTokenGenerator,
            IAccountRepository accountRepository,
            ILogger<ForgotPasswordCommandHandler> logger,
            IdentifierFactory identifierFactory,
            IUnitOfWork uow,
            ITokenRepository tokenRepository,
            ITokenFactory tokenFactory,
            IOutboxRepository outboxCommands,
            IConfiguration configuration)
        {
            _passwordResetTokenGenerator = passwordResetTokenGenerator;
            _accountRepository = accountRepository;
            _logger = logger;
            _identifierFactory = identifierFactory;
            _uow = uow;
            _tokenRepository = tokenRepository;
            _tokenFactory = tokenFactory;
            _outboxCommands = outboxCommands;
            _configuration = configuration;
        }

        public async Task<Result> Handle(ForgotPasswordCommand request, CancellationToken cancellationToken)
        {
            _logger.LogInformation("{@LogCode} | Ident: {Ident}",
                AccountLogs.ForgotPassword_Started,
                request.Value);

            var result = _identifierFactory.Create(request.Type, request.Value);
            if (result.IsFailure)
            {
                _logger.LogWarning("{@LogCode} | Ident: {Ident} | Error: {Error}",
                    AccountLogs.ForgotPassword_InvalidIdentifier,
                    request.Value, result.Error);

                return Result<string>.Failure(result.Error);
            }

            var acc = await _accountRepository.GetByIdentifierWithoutUserAsync(request.Type, result.Value.NormalizedValue, cancellationToken);
            if (acc is null)
            {
                _logger.LogWarning("{@LogCode} | Ident: {Ident}",
                    AccountLogs.ForgotPassword_IdentifierNotFound,
                    request.Value);

                return Result<string>.Failure(AccountErrors.IdentifierNotFound);
            }

            if (acc.IsDeleted)
            {
                _logger.LogWarning("{@LogCode} | AccountId: {AccountId}",
                    AccountLogs.ChangePassword_AccountDeleted,
                    acc.Id);

                return Result.Failure(AccountErrors.AccountDeleted);
            }

            if (!acc.IsActive)
            {
                _logger.LogWarning("{@LogCode} | AccountId: {AccountId}",
                    AccountLogs.ChangePassword_AccountDisabled,
                    acc.Id);

                return Result.Failure(AccountErrors.AccountDisabled);
            }

            string resetToken = _passwordResetTokenGenerator.Generate(acc.Id.ToString());
            if (string.IsNullOrWhiteSpace(resetToken))
            {
                _logger.LogWarning("{@LogCode} | Ident: {Ident}",
                    AccountLogs.ForgotPassword_TokenGeneratedFailed,
                    request.Value);

                return Result<string>.Failure(TokenErrors.TokenGenerationFailed);
            }

            _logger.LogInformation("{@LogCode} | AccountId: {AccountId}",
                AccountLogs.ForgotPassword_TokenGenerated,
                acc.Id);

            var domainToken = _tokenFactory.Create(acc.Id, resetToken, TokenType.ResetPassword);
            await _tokenRepository.AddAsync(domainToken, cancellationToken);

            var devBaseUrl = _configuration["BaseUrl:DevBaseUrl"];

            if (string.IsNullOrEmpty(devBaseUrl))
            {
                _logger.LogError("{@LogCode} | Key: {Key}", CommonLogs.System_ConfigMissing, "BaseUrl:DevBaseUrl");
                return Result.Failure(CommonErrors.SystemConfigurationError);
            }

            var resetLink = $"{devBaseUrl}/control-hub/reset-password?token={domainToken.Value}";
            var payload = new
            {
                To = request.Value,
                Subject = "Reset your password",
                Body = $"Click this link to reset your password: <a href='{resetLink}'>Reset Password</a>"
            };

            var outboxMessage = OutboxMessage.Create(
                OutboxMessageType.Email,
                JsonSerializer.Serialize(payload)
            );
            await _outboxCommands.AddAsync(outboxMessage, cancellationToken);

            await _uow.CommitAsync(cancellationToken);

            _logger.LogInformation("{@LogCode} | Ident: {Ident}",
                AccountLogs.ForgotPassword_NotificationSent,
                request.Value);

            return Result.Success();
        }
    }
}
