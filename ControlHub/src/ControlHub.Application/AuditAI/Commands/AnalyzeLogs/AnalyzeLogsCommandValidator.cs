using FluentValidation;

namespace ControlHub.Application.AuditAI.Commands.AnalyzeLogs
{
    public class AnalyzeLogsCommandValidator : AbstractValidator<AnalyzeLogsCommand>
    {
        public AnalyzeLogsCommandValidator()
        {
            RuleFor(x => x.Query)
                .NotEmpty().WithMessage("Query is required.")
                .MaximumLength(1000).WithMessage("Query must not exceed 1000 characters.");
        }
    }
}
