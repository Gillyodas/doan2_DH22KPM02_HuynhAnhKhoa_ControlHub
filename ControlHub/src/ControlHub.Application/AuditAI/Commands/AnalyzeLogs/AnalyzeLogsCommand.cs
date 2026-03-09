using ControlHub.SharedKernel.Results;
using MediatR;

namespace ControlHub.Application.AuditAI.Commands.AnalyzeLogs
{
    public record AnalyzeLogsCommand(string Query, string? Language = "en") : IRequest<Result<string>>;
}
