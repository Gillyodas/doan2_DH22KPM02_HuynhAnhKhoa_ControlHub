using ControlHub.Application.Accounts.Interfaces.Repositories;
using ControlHub.Application.Common.Persistence;
using ControlHub.Application.Tokens.Interfaces.Repositories;
using ControlHub.Domain.Identity.Security;
using ControlHub.Domain.Identity.ValueObjects;
using ControlHub.SharedKernel.Accounts;
using ControlHub.SharedKernel.Results;
using MediatR;
using Microsoft.Extensions.Logging;

namespace ControlHub.Application.Accounts.Commands.ChangePassword
{
    public class ChangePasswordCommandHandler : IRequestHandler<ChangePasswordCommand, Result>
    {
        private readonly IAccountRepository _accountRepository;
        private readonly IPasswordHasher _passwordHasher;
        private readonly ILogger<ChangePasswordCommandHandler> _logger;
        private readonly IUnitOfWork _uow;
        private readonly ITokenRepository _tokenRepository;

        public ChangePasswordCommandHandler(
            IAccountRepository accountRepository,
            IPasswordHasher passwordHasher,
            ILogger<ChangePasswordCommandHandler> logger,
            IUnitOfWork uow,
            ITokenRepository tokenRepository)
        {
            _accountRepository = accountRepository;
            _passwordHasher = passwordHasher;
            _logger = logger;
            _uow = uow;
            _tokenRepository = tokenRepository;
        }

        public async Task<Result> Handle(ChangePasswordCommand request, CancellationToken cancellationToken)
        {
            _logger.LogInformation("{@LogCode} | AccountId: {AccountId}",
                AccountLogs.ChangePassword_Started,
                request.id);

            var acc = await _accountRepository.GetWithoutUserByIdAsync(request.id, cancellationToken);
            if (acc is null)
            {
                _logger.LogWarning("{@LogCode} | AccountId: {AccountId}",
                    AccountLogs.ChangePassword_AccountNotFound,
                    request.id);

                return Result.Failure(AccountErrors.AccountNotFound);
            }

            if (acc.IsDeleted)
            {
                _logger.LogWarning("{@LogCode} | AccountId: {AccountId}",
                    AccountLogs.ChangePassword_AccountDeleted,
                    request.id);

                return Result.Failure(AccountErrors.AccountDeleted);
            }

            if (!acc.IsActive)
            {
                _logger.LogWarning("{@LogCode} | AccountId: {AccountId}",
                    AccountLogs.ChangePassword_AccountDisabled,
                    request.id);

                return Result.Failure(AccountErrors.AccountDisabled);
            }

            var passIsVerify = _passwordHasher.Verify(request.curPassword, acc.Password);
            if (!passIsVerify)
            {
                _logger.LogWarning("{@LogCode} | AccountId: {AccountId}",
                    AccountLogs.ChangePassword_InvalidPassword,
                    request.id);
                return Result.Failure(AccountErrors.InvalidCredentials);
            }

            if (_passwordHasher.Verify(request.newPassword, acc.Password))
            {
                _logger.LogWarning("{@LogCode} | AccountId: {AccountId}",
                    AccountLogs.ChangePassword_PasswordSameAsOld,
                    request.id);
                return Result.Failure(AccountErrors.PasswordSameAsOld);
            }

            var newPass = Password.Create(request.newPassword, _passwordHasher);
            if (newPass.IsFailure)
            {
                _logger.LogWarning("{@LogCode} | AccountId: {AccountId}",
                    AccountLogs.ChangePassword_PasswordHashFailed,
                    request.id);
                return Result.Failure(newPass.Error);
            }

            var updateResult = acc.UpdatePassword(newPass.Value);
            if (!updateResult.IsSuccess)
            {
                _logger.LogError("{@LogCode} | AccountId: {AccountId} | Errors: {Errors}",
                    AccountLogs.ChangePassword_UpdateFailed,
                    request.id,
                    updateResult.Error);
                return updateResult;
            }

            var tokens = await _tokenRepository.GetTokensByAccountIdAsync(acc.Id, cancellationToken);

            if (tokens.Any())
            {
                foreach (var token in tokens)
                {
                    if (token.IsValid())
                    {
                        token.Revoke();
                    }
                }
            }

            await _uow.CommitAsync(cancellationToken);

            _logger.LogInformation("{@LogCode} | AccountId: {AccountId}",
                AccountLogs.ChangePassword_Success,
                request.id);

            return Result.Success();
        }
    }
}
