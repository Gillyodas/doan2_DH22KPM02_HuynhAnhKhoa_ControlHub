using ControlHub.Domain.Identity.Enums;
using ControlHub.SharedKernel.Common.Errors;
using ControlHub.SharedKernel.Results;

namespace ControlHub.Domain.Identity.Identifiers
{
    public class IdentifierConfig
    {
        public Guid Id { get; private set; }
        public string Name { get; private set; } // e.g., "Employee ID", "Student Code"
        public string Description { get; private set; }
        public bool IsActive { get; private set; }
        public DateTime CreatedAt { get; private set; }
        public DateTime? UpdatedAt { get; private set; }

        // Owned Collection
        private readonly List<ValidationRule> _rules = new();
        public IReadOnlyCollection<ValidationRule> Rules => _rules.AsReadOnly();

        private IdentifierConfig() { } // EF Core

        private IdentifierConfig(Guid id, string name, string description)
        {
            Id = id;
            Name = name;
            Description = description;
            IsActive = true;
            CreatedAt = DateTime.UtcNow;
        }

        public static IdentifierConfig Create(string name, string description)
            => new(Guid.NewGuid(), name, description);

        public Result AddRule(ValidationRuleType type, Dictionary<string, object> parameters)
        {
            var rule = ValidationRule.Create(type, parameters);
            if (rule.IsFailure) return rule;

            _rules.Add(rule.Value);
            UpdatedAt = DateTime.UtcNow;
            return Result.Success();
        }

        public Result RemoveRule(Guid ruleId)
        {
            var rule = _rules.FirstOrDefault(r => r.Id == ruleId);
            if (rule == null) return Result.Failure(Error.NotFound("NOT_FOUND", "Rule not found"));

            _rules.Remove(rule);
            UpdatedAt = DateTime.UtcNow;
            return Result.Success();
        }

        public void Activate()
        {
            IsActive = true;
            UpdatedAt = DateTime.UtcNow;
        }

        public void Deactivate()
        {
            IsActive = false;
            UpdatedAt = DateTime.UtcNow;
        }

        public void UpdateName(string name)
        {
            Name = name;
            UpdatedAt = DateTime.UtcNow;
        }

        public void UpdateDescription(string description)
        {
            Description = description;
            UpdatedAt = DateTime.UtcNow;
        }

        public void UpdateRules(List<ValidationRule> rules)
        {
            _rules.Clear();
            _rules.AddRange(rules);
            UpdatedAt = DateTime.UtcNow;
        }
    }
}
