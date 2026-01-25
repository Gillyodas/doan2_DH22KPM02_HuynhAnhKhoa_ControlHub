using ControlHub.Application.Accounts.Interfaces;
using ControlHub.Application.Accounts.Interfaces.Repositories;
using ControlHub.Application.Common.Persistence;
using ControlHub.SharedKernel.Accounts;
using ControlHub.SharedKernel.Common.Logs;
using ControlHub.SharedKernel.Common.Errors;
using ControlHub.SharedKernel.Results;
using MediatR;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using ControlHub.SharedKernel.Constants;

namespace ControlHub.Application.Accounts.Commands.RegisterAdmin
{
    public class RegisterAdminCommandHandler : IRequestHandler<RegisterAdminCommand, Result<Guid>>
    {
        private readonly IAccountValidator _accountValidator;
        private readonly IAccountRepository _accountRepository;
        private readonly ILogger<RegisterAdminCommandHandler> _logger;
        private readonly IAccountFactory _accountFactory;
        private readonly IConfiguration _config;
        private readonly IUnitOfWork _uow;

        public RegisterAdminCommandHandler(
            IAccountValidator accountValidator,
            IAccountRepository accountRepository,
            ILogger<RegisterAdminCommandHandler> logger,
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

        public async Task<Result<Guid>> Handle(RegisterAdminCommand request, CancellationToken cancellationToken)
        {
            _logger.LogInformation("{@LogCode} | Ident: {Ident}",
                AccountLogs.RegisterAdmin_Started,
                request.Value);

            if (await _accountValidator.IdentifierIsExist(request.Value.ToLower(), request.Type, cancellationToken))
            {
                _logger.LogWarning("{@LogCode} | Ident: {Ident}",
                    AccountLogs.RegisterAdmin_IdentifierExists,
                    request.Value);

                return Result<Guid>.Failure(AccountErrors.EmailAlreadyExists);
            }

            var accId = Guid.NewGuid();

            var roleIdString = _config["RoleSettings:AdminRoleId"];
            if (!Guid.TryParse(roleIdString, out var userRoleId))
            {
                _logger.LogInformation("{@LogCode} | Key: {Key}", CommonLogs.System_ConfigFallback, "RoleSettings:AdminRoleId");
                userRoleId = ControlHubDefaults.Roles.AdminId;
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
                    AccountLogs.RegisterAdmin_FactoryFailed,
                    request.Value,
                    accountResult.Error);

                return Result<Guid>.Failure(accountResult.Error);
            }

            await _accountRepository.AddAsync(accountResult.Value.Value, cancellationToken);

            _logger.LogInformation("{@LogCode} | AccountId: {AccountId} | Ident: {Ident}",
                AccountLogs.RegisterAdmin_Success,
                accId,
                request.Value);

            await _uow.CommitAsync(cancellationToken);

            return Result<Guid>.Success(accId);
        }
    }
}
