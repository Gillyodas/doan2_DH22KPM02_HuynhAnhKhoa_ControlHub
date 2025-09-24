using ControlHub.Application.Accounts.Interfaces.Repositories;
using ControlHub.Application.Accounts.Interfaces.Security;
using ControlHub.Domain.Accounts;
using ControlHub.Domain.Accounts.ValueObjects;
using ControlHub.SharedKernel.Accounts;
using ControlHub.SharedKernel.Common.Logs;
using ControlHub.SharedKernel.Results;
using MediatR;
using Microsoft.Extensions.Logging;

namespace ControlHub.Application.Accounts.Commands.ChangePassword
{
    public class ChangePasswordCommandHandler : IRequestHandler<ChangePasswordCommand, Result>
    {
        private readonly IAccountQueries _accountQueries;
        private readonly IAccountCommands _accountCommands;
        private readonly IPasswordHasher _passwordHasher;
        private readonly ILogger<ChangePasswordCommandHandler> _logger;

        public ChangePasswordCommandHandler(
            IAccountQueries accountQueries,
            IAccountCommands accountCommands,
            IPasswordHasher passwordHasher,
            ILogger<ChangePasswordCommandHandler> logger)
        {
            _accountQueries = accountQueries;
            _accountCommands = accountCommands;
            _passwordHasher = passwordHasher;
            _logger = logger;
        }

        public async Task<Result> Handle(ChangePasswordCommand request, CancellationToken cancellationToken)
        {
            _logger.LogInformation("{Code}: {Message} for AccountId {AccountId}",
                AccountLogs.ChangePassword_Started.Code,
                AccountLogs.ChangePassword_Started.Message,
                request.id);

            Account? acc = await _accountQueries.GetAccountWithoutUserById(request.id, cancellationToken);
            if (acc is null)
            {
                _logger.LogWarning("{Code}: {Message} for AccountId {AccountId}",
                    AccountLogs.ChangePassword_AccountNotFound.Code,
                    AccountLogs.ChangePassword_AccountNotFound.Message,
                    request.id);

                return Result.Failure(AccountErrors.AccountNotFound.Code);
            }

            var passIsVerify = _passwordHasher.Verify(request.curPassword, acc.Password);
            if (!passIsVerify)
            {
                _logger.LogWarning("{Code}: {Message} for AccountId {AccountId}",
                    AccountLogs.ChangePassword_InvalidPassword.Code,
                    AccountLogs.ChangePassword_InvalidPassword.Message,
                    request.id);
                return Result.Failure(AccountErrors.InvalidCredentials.Code);
            }

            Password newPass = _passwordHasher.Hash(request.newPassword);

            var updateResult = acc.UpdatePassword(newPass);
            if (!updateResult.IsSuccess)
            {
                _logger.LogError("{Code}: {Message} for AccountId {AccountId}. Errors: {Errors}",
                    AccountLogs.ChangePassword_UpdateFailed.Code,
                    AccountLogs.ChangePassword_UpdateFailed.Message,
                    request.id,
                    updateResult.Error);
                return updateResult;
            }

            await _accountCommands.UpdateAsync(acc, cancellationToken);

            _logger.LogInformation("{Code}: {Message} for AccountId {AccountId}",
                AccountLogs.ChangePassword_Success.Code,
                AccountLogs.ChangePassword_Success.Message,
                request.id);

            return Result.Success();
        }
    }
}
