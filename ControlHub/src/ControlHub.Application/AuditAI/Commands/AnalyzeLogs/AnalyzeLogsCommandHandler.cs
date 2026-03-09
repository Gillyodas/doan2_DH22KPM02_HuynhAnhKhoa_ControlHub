using ControlHub.Application.AuditAI.Interfaces.V3;
using ControlHub.SharedKernel.Results;
using MediatR;
using Microsoft.Extensions.Logging;

namespace ControlHub.Application.AuditAI.Commands.AnalyzeLogs
{
    public class AnalyzeLogsCommandHandler : IRequestHandler<AnalyzeLogsCommand, Result<string>>
    {
        private readonly IAuditAgentV3 _auditAgent;
        private readonly ILogger<AnalyzeLogsCommandHandler> _logger;

        public AnalyzeLogsCommandHandler(IAuditAgentV3 auditAgent, ILogger<AnalyzeLogsCommandHandler> logger)
        {
            _auditAgent = auditAgent;
            _logger = logger;
        }

        public async Task<Result<string>> Handle(AnalyzeLogsCommand request, CancellationToken cancellationToken)
        {
            _logger.LogInformation("AuditAI | AnalyzeLogs | Query: {Query}", request.Query);

            var result = await _auditAgent.InvestigateAsync(request.Query, null, cancellationToken);

            _logger.LogInformation("AuditAI | AnalyzeLogs | Completed | Confidence: {Confidence}", result.Confidence);

            return Result<string>.Success(result.Answer);
        }
    }
}
