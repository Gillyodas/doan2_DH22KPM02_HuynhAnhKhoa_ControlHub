using ControlHub.Application.Accounts.Interfaces;
using ControlHub.Application.Accounts.Interfaces.Repositories;
using ControlHub.Application.Accounts.Interfaces.Security;
using ControlHub.Domain.Accounts.ValueObjects;
using ControlHub.Domain.Common.Factories;
using ControlHub.SharedKernel.Accounts;
using ControlHub.SharedKernel.Results;
using MediatR;
using Microsoft.Extensions.Logging;

namespace ControlHub.Application.Accounts.Commands.CreateAccount
{
    public class CreateAccountCommandHandler : IRequestHandler<CreateAccountCommand, Result<Guid>>
    {
        private readonly IAccountValidator _accountValidator;
        private readonly IAccountCommands _accountCommands;
        private readonly IPasswordHasher _passwordHasher;
        private readonly ILogger<CreateAccountCommandHandler> _logger;

        public CreateAccountCommandHandler(
            IAccountValidator accountValidator,
            IAccountCommands accountCommands,
            IPasswordHasher passwordHasher,
            ILogger<CreateAccountCommandHandler> logger)
        {
            _accountValidator = accountValidator;
            _accountCommands = accountCommands;
            _passwordHasher = passwordHasher;
            _logger = logger;
        }

        public async Task<Result<Guid>> Handle(CreateAccountCommand request, CancellationToken cancellationToken)
        {
            _logger.LogInformation("{Code}: {Message} for Email {Email}",
                AccountLogs.CreateAccount_Started.Code,
                AccountLogs.CreateAccount_Started.Message,
                request.Email);

            var emailResult = Email.Create(request.Email);
            if (!emailResult.IsSuccess)
            {
                _logger.LogWarning("{Code}: {Message} for Email {Email}",
                    AccountLogs.CreateAccount_InvalidEmail.Code,
                    AccountLogs.CreateAccount_InvalidEmail.Message,
                    request.Email);

                return Result<Guid>.Failure(emailResult.Error);
            }

            var email = emailResult.Value;

            if (await _accountValidator.EmailIsExistAsync(email, cancellationToken))
            {
                _logger.LogWarning("{Code}: {Message} for Email {Email}",
                    AccountLogs.CreateAccount_EmailExists.Code,
                    AccountLogs.CreateAccount_EmailExists.Message,
                    request.Email);

                return Result<Guid>.Failure(AccountErrors.EmailAlreadyExists.Code);
            }

            var accId = Guid.NewGuid();

            var passwordHashResult = _passwordHasher.Hash(request.Password);

            var accountResult = AccountFactory.CreateWithUser(accId, email, passwordHashResult);

            if (!accountResult.IsSuccess)
            {
                _logger.LogError("{Code}: {Message} for Email {Email}. Error: {Error}",
                    AccountLogs.CreateAccount_FactoryFailed.Code,
                    AccountLogs.CreateAccount_FactoryFailed.Message,
                    request.Email,
                    accountResult.Error);

                return Result<Guid>.Failure(accountResult.Error);
            }

            await _accountCommands.AddAsync(accountResult.Value.Value, cancellationToken);

            _logger.LogInformation("{Code}: {Message} for AccountId {AccountId}, Email {Email}",
                AccountLogs.CreateAccount_Success.Code,
                AccountLogs.CreateAccount_Success.Message,
                accId,
                request.Email);

            return Result<Guid>.Success(accId);
        }
    }
}
