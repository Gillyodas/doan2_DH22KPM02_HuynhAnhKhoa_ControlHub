using ControlHub.Application.Accounts.Commands.CreateAccount;
using ControlHub.Application.Accounts.Interfaces;
using ControlHub.Application.Accounts.Interfaces.Repositories;
using ControlHub.Application.Common.Persistence;
using ControlHub.SharedKernel.Accounts;
using ControlHub.SharedKernel.Common.Errors;
using ControlHub.SharedKernel.Results;
using MediatR;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace ControlHub.Application.Accounts.Commands.RegisterUser
{
    public class RegisterUserCommandHandler : IRequestHandler<RegisterUserCommand, Result<Guid>>
    {
        private readonly IAccountValidator _accountValidator;
        private readonly IAccountRepository _accountRepository;
        private readonly ILogger<RegisterUserCommandHandler> _logger;
        private readonly IAccountFactory _accountFactory;
        private readonly IConfiguration _config;
        private readonly IUnitOfWork _uow;

        public RegisterUserCommandHandler(
            IAccountValidator accountValidator,
            IAccountRepository accountRepository,
            ILogger<RegisterUserCommandHandler> logger,
            IAccountFactory accountFactory,
            IConfiguration config,
            IUnitOfWork uow)
        {
            _accountValidator = accountValidator;
            _accountRepository = accountRepository;
            _logger = logger;
            _accountFactory = accountFactory;
            _config = config;
            _uow = uow;
        }

        public async Task<Result<Guid>> Handle(RegisterUserCommand request, CancellationToken cancellationToken)
        {
            _logger.LogInformation("{@LogCode} | Ident: {Ident}",
                AccountLogs.RegisterUser_Started,
                request.Value);

            if (await _accountValidator.IdentifierIsExist(request.Value.ToLower(), request.Type, cancellationToken))
            {
                _logger.LogWarning("{@LogCode} | Ident: {Ident}",
                    AccountLogs.RegisterUser_IdentifierExists,
                    request.Value);

                return Result<Guid>.Failure(AccountErrors.EmailAlreadyExists);
            }

            var accId = Guid.NewGuid();

            var roleIdString = _config["RoleSettings:UserRoleId"];
            if (!Guid.TryParse(roleIdString, out var userRoleId))
            {
                _logger.LogError("Invalid User Role ID configuration: {Value}", roleIdString);
                return Result<Guid>.Failure(CommonErrors.SystemConfigurationError);
            }

            var accountResult = await _accountFactory.CreateWithUserAndIdentifierAsync(
                accId,
                request.Value,
                request.Type,
                request.Password,
                userRoleId,
                identifierConfigId: request.IdentifierConfigId);

            if (!accountResult.IsSuccess)
            {
                _logger.LogError("{@LogCode} | Ident: {Ident} | Error: {Error}",
                    AccountLogs.RegisterUser_FactoryFailed,
                    request.Value,
                    accountResult.Error);

                return Result<Guid>.Failure(accountResult.Error);
            }

            await _accountRepository.AddAsync(accountResult.Value.Value, cancellationToken);

            await _uow.CommitAsync(cancellationToken);

            _logger.LogInformation("{@LogCode} | AccountId: {AccountId} | Ident: {Ident}",
                AccountLogs.RegisterUser_Success,
                accId,
                request.Value);

            return Result<Guid>.Success(accId);
        }
    }
}
