using ControlHub.Application.Accounts.DTOs;
using AppIdentifierConfigRepository = ControlHub.Application.Accounts.Interfaces.Repositories.IIdentifierConfigRepository;
using ControlHub.Domain.Identity.Identifiers;
using ControlHub.SharedKernel.Results;
using MediatR;
using Microsoft.Extensions.Logging;
using ControlHub.SharedKernel.Accounts;

namespace ControlHub.Application.Accounts.Queries.GetActiveIdentifierConfigs
{
    public class GetActiveIdentifierConfigsQueryHandler : IRequestHandler<GetActiveIdentifierConfigsQuery, Result<List<IdentifierConfigDto>>>
    {
        private readonly AppIdentifierConfigRepository _repository;
        private readonly ILogger<GetActiveIdentifierConfigsQueryHandler> _logger;

        public GetActiveIdentifierConfigsQueryHandler(
            AppIdentifierConfigRepository repository,
            ILogger<GetActiveIdentifierConfigsQueryHandler> logger)
        {
            _repository = repository;
            _logger = logger;
        }

        public async Task<Result<List<IdentifierConfigDto>>> Handle(GetActiveIdentifierConfigsQuery request, CancellationToken cancellationToken)
        {
            _logger.LogInformation("{@LogCode} | IncludeDeactivated: {IncludeDeactivated}",
                IdentifierConfigLogs.GetConfigs_Started,
                request.IncludeDeactivated);

            List<IdentifierConfig> allConfigs = new List<IdentifierConfig>();

            // Get active configs
            var activeConfigsResult = await _repository.GetActiveConfigsAsync(cancellationToken);
            if (activeConfigsResult.IsFailure)
            {
                _logger.LogWarning("{@LogCode} | Stage: {Stage} | Error: {Error}",
                    IdentifierConfigLogs.GetConfigs_Failed,
                    "Active",
                    activeConfigsResult.Error.Code);
                return Result<List<IdentifierConfigDto>>.Failure(activeConfigsResult.Error);
            }
            allConfigs.AddRange(activeConfigsResult.Value);

            // Get deactivated configs if requested
            if (request.IncludeDeactivated)
            {
                var deactiveConfigsResult = await _repository.GetDeactiveConfigsAsync(cancellationToken);
                if (deactiveConfigsResult.IsFailure)
                {
                    _logger.LogWarning("{@LogCode} | Stage: {Stage} | Error: {Error}",
                        IdentifierConfigLogs.GetConfigs_Failed,
                        "Deactive",
                        deactiveConfigsResult.Error.Code);
                    return Result<List<IdentifierConfigDto>>.Failure(deactiveConfigsResult.Error);
                }
                allConfigs.AddRange(deactiveConfigsResult.Value);
            }

            var dtos = allConfigs.Select(c => new IdentifierConfigDto(
                c.Id,
                c.Name,
                c.Description,
                c.IsActive,
                c.Rules.Select(r => new ValidationRuleDto(
                    r.Type,
                    System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, object>>(r.ParametersJson) ?? new Dictionary<string, object>(),
                    r.ErrorMessage,
                    r.Order
                )).ToList()
            )).ToList();

            _logger.LogInformation("{@LogCode} | Count: {Count}",
                IdentifierConfigLogs.GetConfigs_Success,
                dtos.Count);

            return Result<List<IdentifierConfigDto>>.Success(dtos);
        }
    }
}
