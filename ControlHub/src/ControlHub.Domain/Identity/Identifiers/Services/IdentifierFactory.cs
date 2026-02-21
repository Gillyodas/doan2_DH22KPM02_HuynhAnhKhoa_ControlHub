using ControlHub.Domain.Identity.Enums;
using ControlHub.Domain.Identity.Identifiers.Rules;
using ControlHub.Domain.Identity.ValueObjects;
using ControlHub.SharedKernel.Accounts;
using ControlHub.SharedKernel.Common.Errors;
using ControlHub.SharedKernel.Results;

namespace ControlHub.Domain.Identity.Identifiers.Services
{
    public class IdentifierFactory
    {
        private readonly IEnumerable<IIdentifierValidator> _validators;
        private readonly IIdentifierConfigRepository _configRepository;
        private readonly DynamicIdentifierValidator _dynamicValidator;

        public IdentifierFactory(
            IEnumerable<IIdentifierValidator> validators,
            IIdentifierConfigRepository configRepository,
            DynamicIdentifierValidator dynamicValidator)
        {
            _validators = validators;
            _configRepository = configRepository;
            _dynamicValidator = dynamicValidator;
        }

        public async Task<Result<Identifier>> CreateAsync(
            IdentifierType type,
            string rawValue,
            Guid? configId = null,
            CancellationToken ct = default)
        {
            // 1. If we have a specific config ID, use dynamic validation
            if (configId.HasValue)
            {
                var configResult = await _configRepository.GetByIdAsync(configId.Value, ct);
                if (configResult.IsFailure)
                    return Result<Identifier>.Failure(Error.NotFound("IdentifierConfig.NotFound", "Identifier configuration not found"));

                var config = configResult.Value;

                var validationResult = _dynamicValidator.ValidateAndNormalize(rawValue, config);
                if (validationResult.IsFailure)
                    return Result<Identifier>.Failure(validationResult.Error);

                return Result<Identifier>.Success(Identifier.CreateWithName(type, config!.Name, rawValue, validationResult.Value));
            }

            // 2. Fallback to strategy-based validation for standard types (Email, Phone, Username)
            var validator = _validators.FirstOrDefault(v => v.Type == type);
            if (validator == null)
                return Result<Identifier>.Failure(AccountErrors.UnsupportedIdentifierType);

            var (isValid, normalized, error) = validator.ValidateAndNormalize(rawValue);
            if (!isValid)
                return Result<Identifier>.Failure(error!);

            return Result<Identifier>.Success(Identifier.Create(type, rawValue, normalized));
        }

        // Keep synchronous version for backward compatibility where possible, but it won't support dynamic configs
        public Result<Identifier> Create(IdentifierType type, string rawValue, Guid? configId = null)
        {
            if (configId.HasValue)
                throw new InvalidOperationException("Dynamic identifier creation requires async call.");

            var validator = _validators.FirstOrDefault(v => v.Type == type);
            if (validator == null)
                return Result<Identifier>.Failure(AccountErrors.UnsupportedIdentifierType);

            var (isValid, normalized, error) = validator.ValidateAndNormalize(rawValue);
            if (!isValid)
                return Result<Identifier>.Failure(error!);

            return Result<Identifier>.Success(Identifier.Create(type, rawValue, normalized));
        }
    }
}
