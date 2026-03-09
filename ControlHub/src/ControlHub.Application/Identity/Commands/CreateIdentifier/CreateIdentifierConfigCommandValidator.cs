using FluentValidation;

namespace ControlHub.Application.Identity.Commands.CreateIdentifier
{
    public class CreateIdentifierConfigCommandValidator : AbstractValidator<CreateIdentifierConfigCommand>
    {
        public CreateIdentifierConfigCommandValidator()
        {
            RuleFor(x => x.Name)
                .NotEmpty().WithMessage("Name is required")
                .MaximumLength(100).WithMessage("Name must not exceed 100 characters");

            RuleFor(x => x.Description)
                .NotEmpty().WithMessage("Description is required")
                .MaximumLength(500).WithMessage("Description must not exceed 500 characters");

            RuleFor(x => x.Rules)
                .NotEmpty().WithMessage("At least one validation rule is required")
                .Must(rules => rules != null && rules.Count > 0)
                .WithMessage("At least one validation rule is required");

            RuleForEach(x => x.Rules).ChildRules(rule =>
            {
                rule.RuleFor(r => r.Type)
                    .NotEmpty().WithMessage("Rule type is required");

                rule.RuleFor(r => r.Order)
                    .GreaterThanOrEqualTo(0).WithMessage("Rule order must be greater than or equal to 0");
            });
        }
    }
}
