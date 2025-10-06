using System.Text.Json;
using ControlHub.Application.Accounts.Interfaces.Repositories;
using ControlHub.Application.Common.Persistence;
using ControlHub.Application.OutBoxs.Repositories;
using ControlHub.Application.Tokens.Interfaces;
using ControlHub.Application.Tokens.Interfaces.Repositories;
using ControlHub.Domain.Accounts.Identifiers.Interfaces;
using ControlHub.Domain.Outboxs;
using ControlHub.Domain.Tokens.Enums;
using ControlHub.SharedKernel.Accounts;
using ControlHub.SharedKernel.Results;
using MediatR;
using Microsoft.Extensions.Logging;

namespace ControlHub.Application.Accounts.Commands.ForgotPassword
{
    public class ForgotPasswordCommandHandler : IRequestHandler<ForgotPasswordCommand, Result>
    {
        private readonly IPasswordResetTokenGenerator _passwordResetTokenGenerator;
        private readonly IAccountQueries _accountQueries;
        private readonly ILogger<ForgotPasswordCommandHandler> _logger;
        private readonly IIdentifierValidatorFactory _identifierValidatorFactory;
        private readonly IUnitOfWork _uow;
        private readonly ITokenCommands _tokenCommands;
        private readonly ITokenFactory _tokenFactory;
        private readonly IOutboxCommands _outboxCommands;

        public ForgotPasswordCommandHandler(
            IPasswordResetTokenGenerator passwordResetTokenGenerator,
            IAccountQueries accountQueries,
            ILogger<ForgotPasswordCommandHandler> logger,
            IIdentifierValidatorFactory identifierValidatorFactory,
            IUnitOfWork uow,
            ITokenCommands tokenCommands,
            ITokenFactory tokenFactory,
            IOutboxCommands outboxCommands)
        {
            _passwordResetTokenGenerator = passwordResetTokenGenerator;
            _accountQueries = accountQueries;
            _logger = logger;
            _identifierValidatorFactory = identifierValidatorFactory;
            _uow = uow;
            _tokenCommands = tokenCommands;
            _tokenFactory = tokenFactory;
            _outboxCommands = outboxCommands;
        }

        public async Task<Result> Handle(ForgotPasswordCommand request, CancellationToken cancellationToken)
        {
            _logger.LogInformation("{Code}: {Message} for Identifier {Ident}",
                AccountLogs.ForgotPassword_Started.Code,
                AccountLogs.ForgotPassword_Started.Message,
                request.Value);

            var validator = _identifierValidatorFactory.Get(request.Type);
            if (validator == null)
            {
                _logger.LogWarning("{Code}: {Message} for IdentifierType {Type}",
                    AccountLogs.ForgotPassword_InvalidIdentifier.Code,
                    AccountLogs.ForgotPassword_InvalidIdentifier.Message,
                    request.Type);

                return Result.Failure(AccountErrors.UnsupportedIdentifierType);
            }

            var (isValid, normalized, error) = validator.ValidateAndNormalize(request.Value);
            if (!isValid)
            {
                _logger.LogWarning("{Code}: {Message} for Identifier {Ident}. Error: {Error}",
                    AccountLogs.ForgotPassword_InvalidIdentifier.Code,
                    AccountLogs.ForgotPassword_InvalidIdentifier.Message,
                    request.Value, error);

                return Result<string>.Failure(error);
            }

            var acc = await _accountQueries.GetByIdentifierWithoutUserAsync(request.Type, normalized, cancellationToken);
            if (acc is null)
            {
                _logger.LogWarning("{Code}: {Message} for Identifier {Ident}",
                    AccountLogs.ForgotPassword_IdentifierNotFound.Code,
                    AccountLogs.ForgotPassword_IdentifierNotFound.Message,
                    request.Value);

                return Result<string>.Failure(AccountErrors.IdentifierNotFound);
            }

            string resetToken = _passwordResetTokenGenerator.Generate(acc.Id.ToString());
            _logger.LogInformation("{Code}: {Message} for AccountId {AccountId}",
                AccountLogs.ForgotPassword_TokenGenerated.Code,
                AccountLogs.ForgotPassword_TokenGenerated.Message,
                acc.Id);

            var domainToken = _tokenFactory.Create(acc.Id, resetToken, TokenType.ResetPassword);
            await _tokenCommands.AddAsync(domainToken, cancellationToken);

            var resetLink = $"https://localhost:7110/swagger/index.html?token={domainToken.Value}";
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

            _logger.LogInformation("{Code}: {Message} for Identifier {Ident}",
                AccountLogs.ForgotPassword_NotificationSent.Code,
                AccountLogs.ForgotPassword_NotificationSent.Message,
                request.Value);

            return Result.Success();
        }
    }
}