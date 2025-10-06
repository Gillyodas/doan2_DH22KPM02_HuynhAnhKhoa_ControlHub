using ControlHub.Application.Accounts.Interfaces;
using ControlHub.Application.Accounts.Interfaces.Repositories;
using ControlHub.Application.Common.Persistence;
using ControlHub.Domain.Accounts.Identifiers.Interfaces;
using ControlHub.Domain.Accounts.Interfaces.Security;
using ControlHub.Domain.Accounts.Services;
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
        private readonly IIdentifierValidatorFactory _identifierValidatorFactory;
        private readonly IUnitOfWork _uow;

        public CreateAccountCommandHandler(
            IAccountValidator accountValidator,
            IAccountCommands accountCommands,
            IPasswordHasher passwordHasher,
            ILogger<CreateAccountCommandHandler> logger,
            IIdentifierValidatorFactory identifierValidatorFactory,
            IUnitOfWork uow)
        {
            _accountValidator = accountValidator;
            _accountCommands = accountCommands;
            _passwordHasher = passwordHasher;
            _logger = logger;
            _identifierValidatorFactory = identifierValidatorFactory;
            _uow = uow;
        }

        public async Task<Result<Guid>> Handle(CreateAccountCommand request, CancellationToken cancellationToken)
        {
            _logger.LogInformation("{Code}: {Message} for Ident {Ident}",
                AccountLogs.CreateAccount_Started.Code,
                AccountLogs.CreateAccount_Started.Message,
                request.Value);

            if (await _accountValidator.IdentifierIsExist(request.Value.ToLower(), request.Type, cancellationToken))
            {
                _logger.LogWarning("{Code}: {Message} for Ident {Ident}",
                    AccountLogs.CreateAccount_IdentifierExists.Code,
                    AccountLogs.CreateAccount_IdentifierExists.Message,
                    request.Value);

                return Result<Guid>.Failure(AccountErrors.EmailAlreadyExists);
            }

            var accId = Guid.NewGuid();

            var accountResult = RegisterService.CreateWithUserAndIdentifier(
                accId,
                request.Value,
                request.Type,
                request.Password,
                _passwordHasher,
                _identifierValidatorFactory);

            if (!accountResult.IsSuccess)
            {
                _logger.LogError("{Code}: {Message} for Ident {Ident}. Error: {Error}",
                    AccountLogs.CreateAccount_FactoryFailed.Code,
                    AccountLogs.CreateAccount_FactoryFailed.Message,
                    request.Value,
                    accountResult.Error);

                return Result<Guid>.Failure(accountResult.Error);
            }

            await _accountCommands.AddAsync(accountResult.Value.Value, cancellationToken);

            _logger.LogInformation("{Code}: {Message} for AccountId {AccountId}, Ident {Ident}",
                AccountLogs.CreateAccount_Success.Code,
                AccountLogs.CreateAccount_Success.Message,
                accId,
                request.Value);

            await _uow.CommitAsync(cancellationToken);

            return Result<Guid>.Success(accId);
        }
    }
}
