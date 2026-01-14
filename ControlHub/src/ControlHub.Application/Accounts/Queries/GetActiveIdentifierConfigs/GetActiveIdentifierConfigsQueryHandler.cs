using ControlHub.Application.Accounts.DTOs;
using ControlHub.Application.Accounts.Interfaces.Repositories;
using ControlHub.Domain.Accounts.Identifiers;
using ControlHub.SharedKernel.Results;
using MediatR;

namespace ControlHub.Application.Accounts.Queries.GetActiveIdentifierConfigs
{
    public class GetActiveIdentifierConfigsQueryHandler : IRequestHandler<GetActiveIdentifierConfigsQuery, Result<List<IdentifierConfigDto>>>
    {
        private readonly IIdentifierConfigRepository _repository;

        public GetActiveIdentifierConfigsQueryHandler(IIdentifierConfigRepository repository)
        {
            _repository = repository;
        }

        public async Task<Result<List<IdentifierConfigDto>>> Handle(GetActiveIdentifierConfigsQuery request, CancellationToken cancellationToken)
        {
            List<IdentifierConfig> allConfigs = new List<IdentifierConfig>();

            // Get active configs
            var activeConfigsResult = await _repository.GetActiveConfigsAsync(cancellationToken);
            if (activeConfigsResult.IsFailure)
            {
                return Result<List<IdentifierConfigDto>>.Failure(activeConfigsResult.Error);
            }
            allConfigs.AddRange(activeConfigsResult.Value);

            // Get deactivated configs if requested
            if (request.IncludeDeactivated)
            {
                var deactiveConfigsResult = await _repository.GetDeactiveConfigsAsync(cancellationToken);
                if (deactiveConfigsResult.IsFailure)
                {
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

            return Result<List<IdentifierConfigDto>>.Success(dtos);
        }
    }
}
