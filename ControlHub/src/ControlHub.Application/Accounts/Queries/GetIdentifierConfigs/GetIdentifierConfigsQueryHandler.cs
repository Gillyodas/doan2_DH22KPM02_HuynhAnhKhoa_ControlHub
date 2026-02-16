using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ControlHub.Application.Accounts.DTOs;
using AppIdentifierConfigRepository = ControlHub.Application.Accounts.Interfaces.Repositories.IIdentifierConfigRepository;
using ControlHub.SharedKernel.Results;
using MediatR;
using Microsoft.Extensions.Logging;
using ControlHub.SharedKernel.Accounts;

namespace ControlHub.Application.Accounts.Queries.GetIdentifierConfigs
{
    public class GetIdentifierConfigsQueryHandler : IRequestHandler<GetIdentifierConfigsQuery, Result<List<IdentifierConfigDto>>>
    {
        private readonly AppIdentifierConfigRepository _repo;
        private readonly ILogger<GetIdentifierConfigsQueryHandler> _logger;

        public GetIdentifierConfigsQueryHandler(
            AppIdentifierConfigRepository repo,
            ILogger<GetIdentifierConfigsQueryHandler> logger)
        {
            _repo = repo;
            _logger = logger;
        }

        public async Task<Result<List<IdentifierConfigDto>>> Handle(
            GetIdentifierConfigsQuery request,
            CancellationToken ct)
        {
            _logger.LogInformation("{@LogCode}", IdentifierConfigLogs.GetConfigs_Started);

            var configsResult = await _repo.GetActiveConfigsAsync(ct);
            if (configsResult.IsFailure)
            {
                _logger.LogWarning("{@LogCode} | Error: {Error}",
                    IdentifierConfigLogs.GetConfigs_Failed,
                    configsResult.Error.Code);
                return Result<List<IdentifierConfigDto>>.Failure(configsResult.Error);
            }

            var dtos = configsResult.Value.Select(c => new IdentifierConfigDto(
                c.Id,
                c.Name,
                c.Description,
                c.IsActive,
                c.Rules.Select(r => new ValidationRuleDto(
                    r.Type,
                    r.GetParameters(),
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
