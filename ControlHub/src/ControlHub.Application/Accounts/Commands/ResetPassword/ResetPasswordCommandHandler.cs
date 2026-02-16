using ControlHub.Application.Accounts.Interfaces.Repositories;
using ControlHub.Application.Common.Persistence;
using ControlHub.Application.Tokens.Interfaces.Repositories;
using ControlHub.Domain.Identity.Security;
using ControlHub.Domain.Identity.ValueObjects;
using ControlHub.SharedKernel.Accounts;
using ControlHub.SharedKernel.Results;
using ControlHub.SharedKernel.Tokens;
using MediatR;
using Microsoft.Extensions.Logging;

namespace ControlHub.Application.Accounts.Commands.ResetPassword
{
    public class ResetPasswordCommandHandler : IRequestHandler<ResetPasswordCommand, Result>
    {
        private readonly IAccountRepository _accountRepository;
        private readonly ILogger<ResetPasswordCommandHandler> _logger;
        private readonly IUnitOfWork _uow;
        private readonly ITokenQueries _tokenQueries;
        private readonly IPasswordHasher _passwordHasher;

        public ResetPasswordCommandHandler(
            IAccountRepository accountRepository,
            ILogger<ResetPasswordCommandHandler> logger,
            IUnitOfWork uow,
            ITokenQueries tokenQueries,
            IPasswordHasher passwordHasher)
        {
            _accountRepository = accountRepository;
            _logger = logger;
            _uow = uow;
            _tokenQueries = tokenQueries;
            _passwordHasher = passwordHasher;
        }
        public async Task<Result> Handle(ResetPasswordCommand request, CancellationToken cancellationToken)
        {
            _logger.LogInformation("{@LogCode} | Token: {Token}",
                AccountLogs.ResetPassword_Started,
                request.Token);

            var token = await _tokenQueries.GetByValueAsync(request.Token, cancellationToken);
            if (token == null)
            {
                _logger.LogWarning("{@LogCode} | Token: {Token}",
                    AccountLogs.ResetPassword_TokenNotFound,
                    request.Token);

                return Result.Failure(TokenErrors.TokenNotFound);
            }

            if (!token.IsValid())
            {
                _logger.LogWarning("{@LogCode} | Token: {Token}",
                    AccountLogs.ResetPassword_TokenInvalid,
                    request.Token);

                return Result.Failure(TokenErrors.TokenInvalid);
            }

            var acc = await _accountRepository.GetWithoutUserByIdAsync(token.AccountId, cancellationToken);
            if (acc == null)
            {
                _logger.LogWarning("{@LogCode} | AccountId: {AccountId}",
                    AccountLogs.ResetPassword_AccountNotFound,
                    token.AccountId);

                return Result.Failure(AccountErrors.AccountNotFound);
            }

            if (acc.IsDeleted)
            {
                _logger.LogWarning("{@LogCode} | AccountId: {AccountId}",
                    AccountLogs.ResetPassword_AccountDisabled,
                    token.AccountId);

                return Result.Failure(AccountErrors.AccountDisabled);
            }

            var pass = Password.Create(request.Password, _passwordHasher);
            if (pass.IsFailure)
            {
                _logger.LogError("{@LogCode} | AccountId: {AccountId} | Error: {Error}",
                    AccountLogs.ResetPassword_PasswordHashFailed,
                    acc.Id,
                    pass.Error);

                return Result.Failure(AccountErrors.PasswordHashFailed);
            }

            acc.UpdatePassword(pass.Value);
            await _uow.CommitAsync(cancellationToken);

            _logger.LogInformation("{@LogCode} | AccountId: {AccountId}",
                AccountLogs.ResetPassword_Success,
                acc.Id);

            return Result.Success();
        }
    }
}
