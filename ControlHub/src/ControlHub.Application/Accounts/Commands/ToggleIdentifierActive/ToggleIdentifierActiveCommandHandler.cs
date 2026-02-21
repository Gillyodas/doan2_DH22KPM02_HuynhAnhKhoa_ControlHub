using ControlHub.Application.Common.Persistence;
using ControlHub.SharedKernel.Accounts;
using ControlHub.SharedKernel.Results;
using MediatR;
using Microsoft.Extensions.Logging;
using AppIdentifierConfigRepository = ControlHub.Application.Accounts.Interfaces.Repositories.IIdentifierConfigRepository;

namespace ControlHub.Application.Accounts.Commands.ToggleIdentifierActive
{
    public class ToggleIdentifierActiveCommandHandler : IRequestHandler<ToggleIdentifierActiveCommand, Result>
    {
        private readonly AppIdentifierConfigRepository _repository;
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<ToggleIdentifierActiveCommandHandler> _logger;

        public ToggleIdentifierActiveCommandHandler(
            AppIdentifierConfigRepository repository,
            IUnitOfWork unitOfWork,
            ILogger<ToggleIdentifierActiveCommandHandler> logger)
        {
            _repository = repository;
            _unitOfWork = unitOfWork;
            _logger = logger;
        }

        public async Task<Result> Handle(ToggleIdentifierActiveCommand request, CancellationToken cancellationToken)
        {
            _logger.LogInformation("{@LogCode} | ConfigId: {ConfigId} | TargetStatus: {TargetStatus}",
                IdentifierConfigLogs.ToggleActive_Started,
                request.Id,
                request.IsActive ? "Active" : "Inactive");

            var configResult = await _repository.GetByIdAsync(request.Id, cancellationToken);
            if (configResult.IsFailure)
            {
                _logger.LogWarning("{@LogCode} | ConfigId: {ConfigId}",
                    IdentifierConfigLogs.ToggleActive_NotFound,
                    request.Id);
                return Result.Failure(configResult.Error);
            }

            var config = configResult.Value;

            if (request.IsActive)
            {
                config.Activate();
            }
            else
            {
                config.Deactivate();
            }

            await _unitOfWork.CommitAsync(cancellationToken);

            _logger.LogInformation("{@LogCode} | ConfigId: {ConfigId} | NewStatus: {NewStatus}",
                IdentifierConfigLogs.ToggleActive_Success,
                request.Id,
                config.IsActive ? "Active" : "Inactive");

            return Result.Success();
        }
    }
}
