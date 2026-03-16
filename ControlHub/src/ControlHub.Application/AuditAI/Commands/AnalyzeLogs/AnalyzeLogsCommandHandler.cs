using ControlHub.Application.AuditAI.Interfaces.V3;
using ControlHub.SharedKernel.Common.Errors;
using ControlHub.SharedKernel.Results;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace ControlHub.Application.AuditAI.Commands.AnalyzeLogs
{
    public class AnalyzeLogsCommandHandler : IRequestHandler<AnalyzeLogsCommand, Result<string>>
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<AnalyzeLogsCommandHandler> _logger;

        public AnalyzeLogsCommandHandler(
            IServiceProvider serviceProvider,
            ILogger<AnalyzeLogsCommandHandler> logger)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
        }

        public async Task<Result<string>> Handle(AnalyzeLogsCommand request, CancellationToken cancellationToken)
        {
            var auditAgent = _serviceProvider.GetService<IAuditAgentV3>();

            if (auditAgent == null)
            {
                _logger.LogWarning("AuditAI V3 is not configured. Set AuditAI:Version to V3.0 in appsettings.json.");
                return Result<string>.Failure(
                    Error.Validation("AuditAI.NotConfigured",
                    "AuditAI V3 module is not enabled. Configure AuditAI:Version=V3.0 to use this feature."));
            }

            _logger.LogInformation("AuditAI | AnalyzeLogs | Query: {Query}", request.Query);
            var result = await auditAgent.InvestigateAsync(request.Query, null, cancellationToken);
            _logger.LogInformation("AuditAI | AnalyzeLogs | Completed | Confidence: {Confidence}", result.Confidence);

            return Result<string>.Success(result.Answer);
        }
    }
}