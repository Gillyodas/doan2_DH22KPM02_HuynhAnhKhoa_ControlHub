using ControlHub.Application.Accounts.Interfaces.Repositories;
using ControlHub.Application.Common.Persistence;
using ControlHub.Application.Tokens.Interfaces.Repositories;
using ControlHub.Domain.Accounts.Interfaces.Security;
using ControlHub.Domain.Accounts.ValueObjects;
using ControlHub.SharedKernel.Accounts;
using ControlHub.SharedKernel.Results;
using ControlHub.SharedKernel.Tokens;
using MediatR;
using Microsoft.Extensions.Logging;

namespace ControlHub.Application.Accounts.Commands.ResetPassword
{
    public class ResetPasswordCommandHandler : IRequestHandler<ResetPasswordCommand, Result>
    {
        private readonly IAccountCommands _accountCommands;
        private readonly ILogger<ResetPasswordCommandHandler> _logger;
        private readonly IUnitOfWork _uow;
        private readonly ITokenQueries _tokenQueries;
        private readonly IAccountQueries _accountQueries;
        private readonly IPasswordHasher _passwordHasher;

        public ResetPasswordCommandHandler(
            IAccountCommands accountCommands,
            ILogger<ResetPasswordCommandHandler> logger,
            IUnitOfWork uow,
            ITokenQueries tokenQueries,
            IAccountQueries accountQueries,
            IPasswordHasher passwordHasher)
        {
            _accountCommands = accountCommands;
            _logger = logger;
            _uow = uow;
            _tokenQueries = tokenQueries;
            _accountQueries = accountQueries;
            _passwordHasher = passwordHasher;
        }
        public async Task<Result> Handle(ResetPasswordCommand request, CancellationToken cancellationToken)
        {
            _logger.LogInformation("{Code}: {Message} for Token {Token}",
                AccountLogs.ResetPassword_Started.Code,
                AccountLogs.ResetPassword_Started.Message,
                request.Token);

            var token = await _tokenQueries.GetByValueAsync(request.Token, cancellationToken);
            if (token == null)
            {
                _logger.LogWarning("{Code}: {Message} for Token {Token}",
                    AccountLogs.ResetPassword_TokenNotFound.Code,
                    AccountLogs.ResetPassword_TokenNotFound.Message,
                    request.Token);

                return Result.Failure(TokenErrors.TokenNotFound);
            }

            if (!token.IsValid())
            {
                _logger.LogWarning("{Code}: {Message} for Token {Token}",
                    AccountLogs.ResetPassword_TokenInvalid.Code,
                    AccountLogs.ResetPassword_TokenInvalid.Message,
                    request.Token);

                return Result.Failure(TokenErrors.TokenInvalid);
            }

            var acc = await _accountQueries.GetWithoutUserByIdAsync(token.AccountId, cancellationToken);
            if (acc == null)
            {
                _logger.LogWarning("{Code}: {Message} for AccountId {AccountId}",
                    AccountLogs.ResetPassword_AccountNotFound.Code,
                    AccountLogs.ResetPassword_AccountNotFound.Message,
                    token.AccountId);

                return Result.Failure(AccountErrors.AccountNotFound);
            }

            if (acc.IsDeleted)
            {
                _logger.LogWarning("{Code}: {Message} for AccountId {AccountId}",
                    AccountLogs.ResetPassword_AccountDisabled.Code,
                    AccountLogs.ResetPassword_AccountDisabled.Message,
                    token.AccountId);

                return Result.Failure(AccountErrors.AccountDisabled);
            }

            var pass = Password.Create(request.Password, _passwordHasher);
            if (pass == null)
            {
                _logger.LogError("{Code}: {Message} for AccountId {AccountId}",
                    AccountLogs.ResetPassword_PasswordHashFailed.Code,
                    AccountLogs.ResetPassword_PasswordHashFailed.Message,
                    acc.Id);

                return Result.Failure(AccountErrors.PasswordHashFailed);
            }

            acc.UpdatePassword(pass);

            await _accountCommands.UpdateAsync(acc, cancellationToken);
            await _uow.CommitAsync();

            _logger.LogInformation("{Code}: {Message} for AccountId {AccountId}",
                AccountLogs.ResetPassword_Success.Code,
                AccountLogs.ResetPassword_Success.Message,
                acc.Id);

            return Result.Success();
        }
    }
}
