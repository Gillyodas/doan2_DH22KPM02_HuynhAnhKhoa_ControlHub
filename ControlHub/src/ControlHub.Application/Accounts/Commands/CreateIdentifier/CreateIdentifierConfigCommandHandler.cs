using ControlHub.Application.Common.Persistence;
using ControlHub.Domain.Identity.Identifiers;
using ControlHub.SharedKernel.Accounts;
using ControlHub.SharedKernel.Common.Errors;
using ControlHub.SharedKernel.Results;
using MediatR;
using Microsoft.Extensions.Logging;
using AppIdentifierConfigRepository = ControlHub.Application.Accounts.Interfaces.Repositories.IIdentifierConfigRepository;

namespace ControlHub.Application.Accounts.Commands.CreateIdentifier
{
    public class CreateIdentifierConfigCommandHandler : IRequestHandler<CreateIdentifierConfigCommand, Result<Guid>>
    {
        private readonly AppIdentifierConfigRepository _identifierConfigRepository;
        private readonly IUnitOfWork _uow;
        private readonly ILogger<CreateIdentifierConfigCommandHandler> _logger;
        public CreateIdentifierConfigCommandHandler(
            AppIdentifierConfigRepository identifierConfigRepository,
            IUnitOfWork uow,
            ILogger<CreateIdentifierConfigCommandHandler> logger)
        {
            _identifierConfigRepository = identifierConfigRepository;
            _uow = uow;
            _logger = logger;
        }
        public async Task<Result<Guid>> Handle(CreateIdentifierConfigCommand request, CancellationToken cancellationToken)
        {
            _logger.LogInformation("{@LogCode} | Name: {Name}",
                IdentifierConfigLogs.CreateConfig_Started,
                request.Name);

            var existingResult = await _identifierConfigRepository.GetByNameAsync(request.Name, cancellationToken);
            if (existingResult.IsSuccess)
            {
                _logger.LogWarning("{@LogCode} | Name: {Name}",
                    IdentifierConfigLogs.CreateConfig_Duplicate,
                    request.Name);
                return Result<Guid>.Failure(Error.Conflict("CONFLICT", "Identifier name already exists"));
            }

            var config = IdentifierConfig.Create(request.Name, request.Description);

            foreach (var ruleDto in request.Rules.OrderBy(r => r.Order))
            {
                var result = config.AddRule(ruleDto.Type, ruleDto.Parameters);

                if (result.IsFailure)
                {
                    _logger.LogWarning("{@LogCode} | RuleType: {RuleType} | Error: {Error}",
                        IdentifierConfigLogs.CreateConfig_RuleFailed,
                        ruleDto.Type,
                        result.Error.Code);
                    return Result<Guid>.Failure(result.Error);
                }
            }

            var addResult = await _identifierConfigRepository.AddAsync(config, cancellationToken);
            if (addResult.IsFailure)
            {
                _logger.LogWarning("{@LogCode} | Error: {Error}",
                    IdentifierConfigLogs.CreateConfig_PersistFailed,
                    addResult.Error.Code);
                return Result<Guid>.Failure(addResult.Error);
            }

            await _uow.CommitAsync(cancellationToken);

            _logger.LogInformation("{@LogCode} | Id: {Id} | Name: {Name}",
                 IdentifierConfigLogs.CreateConfig_Success,
                 config.Id,
                 config.Name);

            return Result<Guid>.Success(config.Id);
        }
    }
}
