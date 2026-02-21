using ControlHub.Application.Accounts.Interfaces;
using ControlHub.Application.Accounts.Interfaces.Repositories;
using ControlHub.Application.Common.Persistence;
using ControlHub.SharedKernel.Accounts;
using ControlHub.SharedKernel.Common.Errors;
using ControlHub.SharedKernel.Common.Logs;
using ControlHub.SharedKernel.Constants;
using ControlHub.SharedKernel.Results;
using MediatR;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace ControlHub.Application.Accounts.Commands.RegisterSupperAdmin
{
    public class RegisterSupperAdminCommandHandler : IRequestHandler<RegisterSupperAdminCommand, Result<Guid>>
    {
        private readonly IAccountValidator _accountValidator;
        private readonly IAccountRepository _accountRepository;
        private readonly ILogger<RegisterSupperAdminCommandHandler> _logger;
        private readonly IAccountFactory _accountFactory;
        private readonly IConfiguration _config;
        private readonly IUnitOfWork _uow;

        public RegisterSupperAdminCommandHandler(
            IAccountValidator accountValidator,
            IAccountRepository accountRepository,
            ILogger<RegisterSupperAdminCommandHandler> logger,
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

        public async Task<Result<Guid>> Handle(RegisterSupperAdminCommand request, CancellationToken cancellationToken)
        {
            _logger.LogInformation("{@LogCode} | Ident: {Ident}",
                AccountLogs.RegisterSupperAdmin_Started,
                request.Value);

            var masterKey = _config["AppPassword:MasterKey"];

            if (string.IsNullOrEmpty(masterKey))
            {
                _logger.LogError("{@LogCode}",
                    CommonLogs.System_ConfigMissing);

                return Result<Guid>.Failure(CommonErrors.SystemConfigurationError);
            }

            if (request.MasterKey != masterKey)
            {
                _logger.LogWarning("{@LogCode} | Email attempted: {Email}",
                    CommonLogs.Auth_InvalidMasterKey,
                    request.Value);

                return Result<Guid>.Failure(CommonErrors.InvalidMasterKey);
            }

            if (await _accountValidator.IdentifierIsExist(request.Value.ToLower(), request.Type, cancellationToken))
            {
                _logger.LogWarning("{@LogCode} | Ident: {Ident}",
                    AccountLogs.RegisterSupperAdmin_IdentifierExists,
                    request.Value);

                return Result<Guid>.Failure(AccountErrors.EmailAlreadyExists);
            }

            var accId = Guid.NewGuid();
            var roleIdConfig = _config["RoleSettings:SuperAdminRoleId"];
            Guid superAdminRoleId;
            if (!Guid.TryParse(roleIdConfig, out superAdminRoleId))
            {
                _logger.LogInformation("RoleSettings:SuperAdminRoleId is missing or invalid. Using Default ID.");
                superAdminRoleId = ControlHubDefaults.Roles.SuperAdminId;
            }

            var accountResult = await _accountFactory.CreateWithUserAndIdentifierAsync(
                accId,
                request.Value,
                request.Type,
                request.Password,
                superAdminRoleId,
                identifierConfigId: request.IdentifierConfigId);

            if (!accountResult.IsSuccess)
            {
                _logger.LogError("{@LogCode} | Ident: {Ident} | Error: {Error}",
                    AccountLogs.RegisterSupperAdmin_FactoryFailed,
                    request.Value,
                    accountResult.Error);

                return Result<Guid>.Failure(accountResult.Error);
            }

            await _accountRepository.AddAsync(accountResult.Value.Value, cancellationToken);

            await _uow.CommitAsync(cancellationToken);

            _logger.LogInformation("{@LogCode} | AccountId: {AccountId} | Ident: {Ident}",
                AccountLogs.RegisterSupperAdmin_Success,
                accId,
                request.Value);

            return Result<Guid>.Success(accId);
        }
    }
}
