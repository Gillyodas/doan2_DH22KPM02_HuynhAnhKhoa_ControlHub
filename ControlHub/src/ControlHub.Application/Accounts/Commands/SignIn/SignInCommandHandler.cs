using ControlHub.Application.Accounts.DTOs;
using ControlHub.Application.Accounts.Interfaces.Repositories;
using ControlHub.Application.Accounts.Interfaces.Security;
using ControlHub.Domain.Accounts;
using ControlHub.Domain.Accounts.ValueObjects;
using ControlHub.SharedKernel.Accounts;
using ControlHub.SharedKernel.Results;
using MediatR;
using Microsoft.Extensions.Logging;

namespace ControlHub.Application.Accounts.Commands.SignIn
{
    public class SignInCommandHandler : IRequestHandler<SignInCommand, Result<SignInDTO>>
    {
        private readonly IPasswordHasher _passwordHasher;
        private readonly IAccountQueries _accountQueries;
        private readonly ILogger<SignInCommandHandler> _logger;

        public SignInCommandHandler(
            IPasswordHasher passwordHasher,
            IAccountQueries accountQueries,
            ILogger<SignInCommandHandler> logger)
        {
            _passwordHasher = passwordHasher;
            _accountQueries = accountQueries;
            _logger = logger;
        }

        public async Task<Result<SignInDTO>> Handle(SignInCommand request, CancellationToken cancellationToken)
        {
            _logger.LogInformation("{Code}: {Message} for Email {Email}",
                AccountLogs.SignIn_Started.Code,
                AccountLogs.SignIn_Started.Message,
                request.email);

            var resultCreateEmail = Email.Create(request.email);
            if (!resultCreateEmail.IsSuccess)
            {
                _logger.LogWarning("{Code}: {Message} for Email {Email}",
                    AccountLogs.SignIn_InvalidEmail.Code,
                    AccountLogs.SignIn_InvalidEmail.Message,
                    request.email);

                return Result<SignInDTO>.Failure(AccountErrors.InvalidEmail.Code);
            }

            var account = await _accountQueries.GetAccountByEmail(resultCreateEmail.Value, cancellationToken);
            if (account is null)
            {
                _logger.LogWarning("{Code}: {Message} for Email {Email}",
                    AccountLogs.SignIn_AccountNotFound.Code,
                    AccountLogs.SignIn_AccountNotFound.Message,
                    request.email);

                return Result<SignInDTO>.Failure(AccountErrors.InvalidCredentials.Code);
            }

            var resultVerifyPassword = _passwordHasher.Verify(request.password, account.Password);
            if (!resultVerifyPassword)
            {
                _logger.LogWarning("{Code}: {Message} for AccountId {AccountId}, Email {Email}",
                    AccountLogs.SignIn_InvalidPassword.Code,
                    AccountLogs.SignIn_InvalidPassword.Message,
                    account.Id,
                    request.email);

                return Result<SignInDTO>.Failure(AccountErrors.InvalidCredentials.Code);
            }

            _logger.LogInformation("{Code}: {Message} for AccountId {AccountId}, Email {Email}",
                AccountLogs.SignIn_Success.Code,
                AccountLogs.SignIn_Success.Message,
                account.Id,
                request.email);

            var dto = new SignInDTO(
                account.Id,
                account.User.Match(some: u => u.Username, none: () => "No name"),
                "fake_jwt_here"
            );

            return Result<SignInDTO>.Success(dto);
        }
    }
}
