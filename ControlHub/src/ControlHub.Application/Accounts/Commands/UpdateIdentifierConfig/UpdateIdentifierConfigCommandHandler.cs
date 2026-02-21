using ControlHub.Application.Common.Persistence;
using ControlHub.Domain.Identity.Identifiers;
using ControlHub.SharedKernel.Accounts;
using ControlHub.SharedKernel.Common.Errors;
using ControlHub.SharedKernel.Results;
using MediatR;
using Microsoft.Extensions.Logging;
using AppIdentifierConfigRepository = ControlHub.Application.Accounts.Interfaces.Repositories.IIdentifierConfigRepository;

namespace ControlHub.Application.Accounts.Commands.UpdateIdentifierConfig
{
    public class UpdateIdentifierConfigCommandHandler : IRequestHandler<UpdateIdentifierConfigCommand, Result>
    {
        private readonly AppIdentifierConfigRepository _repository;
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<UpdateIdentifierConfigCommandHandler> _logger;

        public UpdateIdentifierConfigCommandHandler(
            AppIdentifierConfigRepository repository,
            IUnitOfWork unitOfWork,
            ILogger<UpdateIdentifierConfigCommandHandler> logger)
        {
            _repository = repository;
            _unitOfWork = unitOfWork;
            _logger = logger;
        }

        public async Task<Result> Handle(UpdateIdentifierConfigCommand request, CancellationToken cancellationToken)
        {
            _logger.LogInformation("{@LogCode} | Id: {Id} | Name: {Name}",
                 IdentifierConfigLogs.UpdateConfig_Started,
                 request.Id,
                 request.Name);

            var configResult = await _repository.GetByIdAsync(request.Id, cancellationToken);
            if (configResult.IsFailure)
            {
                _logger.LogWarning("{@LogCode} | Id: {Id}",
                    IdentifierConfigLogs.UpdateConfig_NotFound,
                    request.Id);
                return Result.Failure(configResult.Error);
            }

            var config = configResult.Value;

            // Check if name is being changed and if new name already exists
            if (config.Name != request.Name)
            {
                var existingResult = await _repository.GetByNameAsync(request.Name, cancellationToken);
                if (existingResult.IsSuccess && existingResult.Value.Id != config.Id)
                {
                    _logger.LogWarning("{@LogCode} | Name: {Name}",
                        IdentifierConfigLogs.UpdateConfig_DuplicateName,
                        request.Name);
                    return Result.Failure(new Error("IdentifierConfig.DuplicateName", "An identifier configuration with this name already exists"));
                }
            }

            // Update basic properties
            config.UpdateName(request.Name);
            config.UpdateDescription(request.Description);

            // Update rules
            var validationRules = request.Rules.Select(r =>
                ValidationRule.Create(r.Type, r.Parameters, r.ErrorMessage, r.Order).Value
            ).ToList();

            config.UpdateRules(validationRules);

            await _unitOfWork.CommitAsync(cancellationToken);

            _logger.LogInformation("{@LogCode} | Id: {Id} | Name: {Name}",
                IdentifierConfigLogs.UpdateConfig_Success,
                config.Id,
                config.Name);

            return Result.Success();
        }
    }
}
