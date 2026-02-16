using System.Text.RegularExpressions;
using ControlHub.Domain.Identity.Enums;
using ControlHub.SharedKernel.Common.Errors;
using ControlHub.SharedKernel.Results;

namespace ControlHub.Domain.Identity.Identifiers.Services
{
    public class DynamicIdentifierValidator
    {
        public Result<string> ValidateAndNormalize(
            string rawValue,
            IdentifierConfig config)
        {
            if (!config.IsActive)
                return Result<string>.Failure(Error.Validation("IdentifierDisabled", "Identifier type is disabled"));

            var orderedRules = config.Rules.OrderBy(r => r.Order);

            foreach (var rule in orderedRules)
            {
                var result = ExecuteRule(rawValue, rule);
                if (result.IsFailure) return result;
            }

            // Default normalization if no custom normalizer
            var normalized = rawValue.Trim();

            return Result<string>.Success(normalized);
        }

        private Result<string> ExecuteRule(string value, ValidationRule rule)
        {
            var parameters = rule.GetParameters();
            var errorMsg = rule.ErrorMessage ?? GetDefaultErrorMessage(rule.Type);

            switch (rule.Type)
            {
                case ValidationRuleType.Required:
                    if (string.IsNullOrWhiteSpace(value))
                        return Result<string>.Failure(Error.Validation("Required", errorMsg));
                    break;

                case ValidationRuleType.MinLength:
                    var min = Convert.ToInt32(GetParameterValue(parameters["length"]));
                    if (value.Length < min)
                        return Result<string>.Failure(Error.Validation("MinLength", errorMsg));
                    break;

                case ValidationRuleType.MaxLength:
                    var max = Convert.ToInt32(GetParameterValue(parameters["length"]));
                    if (value.Length > max)
                        return Result<string>.Failure(Error.Validation("MaxLength", errorMsg));
                    break;

                case ValidationRuleType.Pattern:
                    var pattern = GetParameterValue(parameters["pattern"]).ToString()!;
                    var options = parameters.ContainsKey("options")
                        ? (RegexOptions)Convert.ToInt32(GetParameterValue(parameters["options"]))
                        : RegexOptions.None;

                    var regex = new Regex(pattern, options | RegexOptions.Compiled);
                    if (!regex.IsMatch(value))
                        return Result<string>.Failure(Error.Validation("Pattern", errorMsg));
                    break;

                case ValidationRuleType.Email:
                    // Use built-in email validation
                    var emailRegex = new Regex(
                        @"^(\w+(?:[.+\-]\w+)*)@(\w+(?:[.-]\w+)*\.[a-z]{2,})$",
                        RegexOptions.IgnoreCase | RegexOptions.Compiled);
                    if (!emailRegex.IsMatch(value))
                        return Result<string>.Failure(Error.Validation("Email", errorMsg));
                    break;

                case ValidationRuleType.Phone:
                    // Phone validation with customizable pattern
                    var phonePattern = parameters.ContainsKey("pattern")
                        ? GetParameterValue(parameters["pattern"]).ToString()!
                        : @"^(\+?\d{1,3}[-.\s]?)?\(?\d{3}\)?[-.\s]?\d{3}[-.\s]?\d{4}$";

                    var allowInternational = parameters.ContainsKey("allowInternational")
                        ? Convert.ToBoolean(GetParameterValue(parameters["allowInternational"]))
                        : true;

                    var phoneRegex = new Regex(phonePattern, RegexOptions.Compiled);
                    if (!phoneRegex.IsMatch(value))
                        return Result<string>.Failure(Error.Validation("Phone", errorMsg));

                    // Additional international validation if allowed
                    if (allowInternational && value.StartsWith("+"))
                    {
                        var internationalRegex = new Regex(@"^\+\d{1,3}", RegexOptions.Compiled);
                        if (!internationalRegex.IsMatch(value))
                            return Result<string>.Failure(Error.Validation("Phone", "Invalid international phone format"));
                    }
                    break;

                case ValidationRuleType.Range:
                    if (!parameters.ContainsKey("min") || !parameters.ContainsKey("max"))
                        return Result<string>.Failure(Error.Validation("Range", "Missing 'min' or 'max' parameter"));

                    var minValue = Convert.ToDouble(GetParameterValue(parameters["min"]));
                    var maxValue = Convert.ToDouble(GetParameterValue(parameters["max"]));

                    // Check if value is numeric
                    if (!double.TryParse(value, out var numericValue))
                        return Result<string>.Failure(Error.Validation("Range", "Value must be numeric for range validation"));

                    if (numericValue < minValue || numericValue > maxValue)
                        return Result<string>.Failure(Error.Validation("Range", errorMsg));
                    break;

                case ValidationRuleType.Custom:
                    // Custom validation with custom logic
                    if (!parameters.ContainsKey("customLogic"))
                        return Result<string>.Failure(Error.Validation("Custom", "Missing 'customLogic' parameter"));

                    var customLogic = GetParameterValue(parameters["customLogic"]).ToString()!;

                    // Execute custom logic based on the type
                    switch (customLogic.ToLower())
                    {
                        case "uppercase":
                            if (!value.All(char.IsUpper))
                                return Result<string>.Failure(Error.Validation("Custom", "Value must be uppercase"));
                            break;
                        case "lowercase":
                            if (!value.All(char.IsLower))
                                return Result<string>.Failure(Error.Validation("Custom", "Value must be lowercase"));
                            break;
                        case "alphanumeric":
                            if (!value.All(char.IsLetterOrDigit))
                                return Result<string>.Failure(Error.Validation("Custom", "Value must be alphanumeric"));
                            break;
                        case "numeric":
                            if (!value.All(char.IsDigit))
                                return Result<string>.Failure(Error.Validation("Custom", "Value must be numeric"));
                            break;
                        case "letters":
                            if (!value.All(char.IsLetter))
                                return Result<string>.Failure(Error.Validation("Custom", "Value must contain only letters"));
                            break;
                        default:
                            return Result<string>.Failure(Error.Validation("Custom", "Unknown custom logic type"));
                    }
                    break;
            }

            return Result<string>.Success(value);
        }

        private static object GetParameterValue(object parameter)
        {
            // Handle JsonElement from System.Text.Json deserialization
            if (parameter is System.Text.Json.JsonElement element)
            {
                return element.ValueKind switch
                {
                    System.Text.Json.JsonValueKind.String => element.GetString(),
                    System.Text.Json.JsonValueKind.Number => element.GetDouble(),
                    System.Text.Json.JsonValueKind.True or System.Text.Json.JsonValueKind.False => element.GetBoolean(),
                    _ => parameter.ToString()
                };
            }

            return parameter;
        }

        private string GetDefaultErrorMessage(ValidationRuleType type)
        {
            return type switch
            {
                ValidationRuleType.Required => "This field is required",
                ValidationRuleType.MinLength => "Value is too short",
                ValidationRuleType.MaxLength => "Value is too long",
                ValidationRuleType.Pattern => "Invalid format",
                ValidationRuleType.Email => "Invalid email format",
                ValidationRuleType.Phone => "Invalid phone format",
                _ => "Validation failed"
            };
        }
    }
}
