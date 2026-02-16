using System.Text.RegularExpressions;
using ControlHub.Domain.Identity.Enums;
using ControlHub.SharedKernel.Common.Errors;
using ControlHub.SharedKernel.Results;

namespace ControlHub.Domain.Identity.Identifiers
{
    public class ValidationRule
    {
        public Guid Id { get; private set; }
        public ValidationRuleType Type { get; private set; }
        public string ParametersJson { get; private set; } // JSON serialized parameters
        public string? ErrorMessage { get; private set; }
        public int Order { get; private set; } // Execution order

        private ValidationRule() { } // EF Core

        private ValidationRule(
            Guid id,
            ValidationRuleType type,
            string parametersJson,
            string? errorMessage,
            int order)
        {
            Id = id;
            Type = type;
            ParametersJson = parametersJson;
            ErrorMessage = errorMessage;
            Order = order;
        }

        public static Result<ValidationRule> Create(
            ValidationRuleType type,
            Dictionary<string, object> parameters,
            string? errorMessage = null,
            int order = 0)
        {
            // Validate parameters based on type
            var validationResult = ValidateParameters(type, parameters);
            if (validationResult.IsFailure) return Result<ValidationRule>.Failure(validationResult.Error);

            var json = System.Text.Json.JsonSerializer.Serialize(parameters);

            return Result<ValidationRule>.Success(new ValidationRule(
                Guid.NewGuid(),
                type,
                json,
                errorMessage,
                order));
        }

        public Dictionary<string, object> GetParameters()
        {
            return System.Text.Json.JsonSerializer
                .Deserialize<Dictionary<string, object>>(ParametersJson)!;
        }

        private static Result ValidateParameters(
            ValidationRuleType type,
            Dictionary<string, object> parameters)
        {
            switch (type)
            {
                case ValidationRuleType.MinLength:
                case ValidationRuleType.MaxLength:
                    if (!parameters.ContainsKey("length"))
                        return Result.Failure(Error.Validation("MinLengthMaxLength", "Missing 'length' parameter"));
                    break;

                case ValidationRuleType.Pattern:
                    if (!parameters.ContainsKey("pattern"))
                        return Result.Failure(Error.Validation("Pattern", "Missing 'pattern' parameter"));
                    // Validate regex is valid
                    try
                    {
                        _ = new Regex(parameters["pattern"].ToString()!);
                    }
                    catch
                    {
                        return Result.Failure(Error.Validation("Pattern", "Invalid regex pattern"));
                    }
                    break;

                case ValidationRuleType.Range:
                    if (!parameters.ContainsKey("min") || !parameters.ContainsKey("max"))
                        return Result.Failure(Error.Validation("Range", "Missing 'min' or 'max' parameter"));

                    // Validate that min and max are numeric
                    if (!double.TryParse(parameters["min"].ToString(), out _) ||
                        !double.TryParse(parameters["max"].ToString(), out _))
                        return Result.Failure(Error.Validation("Range", "'min' and 'max' must be numeric values"));
                    break;

                case ValidationRuleType.Phone:
                    if (parameters.ContainsKey("pattern"))
                    {
                        // Validate regex pattern if provided
                        try
                        {
                            _ = new Regex(parameters["pattern"].ToString()!);
                        }
                        catch
                        {
                            return Result.Failure(Error.Validation("Phone", "Invalid phone pattern regex"));
                        }
                    }

                    if (parameters.ContainsKey("allowInternational"))
                    {
                        if (!bool.TryParse(parameters["allowInternational"].ToString(), out _))
                            return Result.Failure(Error.Validation("Phone", "allowInternational must be boolean"));
                    }
                    break;

                case ValidationRuleType.Custom:
                    if (!parameters.ContainsKey("customLogic"))
                        return Result.Failure(Error.Validation("Custom", "Missing 'customLogic' parameter"));

                    var validCustomLogics = new[] { "uppercase", "lowercase", "alphanumeric", "numeric", "letters" };
                    var customLogic = parameters["customLogic"].ToString()!;
                    if (!validCustomLogics.Contains(customLogic.ToLower()))
                        return Result.Failure(Error.Validation("Custom", $"Invalid customLogic. Valid options: {string.Join(", ", validCustomLogics)}"));
                    break;
            }

            return Result.Success();
        }
    }
}
