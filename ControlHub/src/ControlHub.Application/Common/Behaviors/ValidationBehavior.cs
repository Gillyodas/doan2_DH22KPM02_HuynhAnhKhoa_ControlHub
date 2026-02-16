using System.Collections.Concurrent;
using System.Reflection;
using ControlHub.SharedKernel.Common.Errors;
using ControlHub.SharedKernel.Results;
using FluentValidation;
using MediatR;

namespace ControlHub.Application.Common.Behaviors
{
    public class ValidationBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
        where TRequest : notnull
    {
        private readonly IEnumerable<IValidator<TRequest>> _validators;
        private static readonly ConcurrentDictionary<Type, MethodInfo?> _failureMethodCache = new();

        public ValidationBehavior(IEnumerable<IValidator<TRequest>> validators)
        {
            _validators = validators ?? Array.Empty<IValidator<TRequest>>();
        }

        public async Task<TResponse> Handle(
            TRequest request,
            RequestHandlerDelegate<TResponse> next,
            CancellationToken cancellationToken)
        {
            if (!_validators.Any())
                return await next();

            var context = new ValidationContext<TRequest>(request);

            var validationResults = await Task.WhenAll(
                _validators.Select(v => v.ValidateAsync(context, cancellationToken))
            );

            var failures = validationResults
                .SelectMany(r => r.Errors)
                .Where(f => f != null)
                .ToList();

            if (failures.Count == 0)
                return await next();

            // Ghép l?i thành Error chu?n
            var errorMessage = string.Join("; ", failures.Select(f => f.ErrorMessage));
            var error = Error.Validation("Validation.Failed", errorMessage);

            // Tru?ng h?p Handler tr? v? Result<T> - use cached reflection
            if (typeof(TResponse).IsGenericType &&
                typeof(TResponse).GetGenericTypeDefinition() == typeof(Result<>))
            {
                var failureMethod = _failureMethodCache.GetOrAdd(typeof(TResponse), responseType =>
                {
                    var genericType = responseType.GetGenericArguments()[0];
                    return typeof(Result<>)
                        .MakeGenericType(genericType)
                        .GetMethods()
                        .FirstOrDefault(m => m.Name == nameof(Result<object>.Failure)
                            && m.GetParameters().FirstOrDefault()?.ParameterType == typeof(Error));
                });

                if (failureMethod == null)
                    throw new InvalidOperationException($"Cannot find Failure(Error) on Result<{typeof(TResponse).GetGenericArguments()[0].Name}>");

                var result = failureMethod.GetParameters().Length == 2
                    ? failureMethod.Invoke(null, new object[] { error, null })
                    : failureMethod.Invoke(null, new object[] { error });
                return (TResponse)result!;
            }

            // Tru?ng h?p Handler tr? v? Result (non-generic)
            if (typeof(TResponse) == typeof(Result))
            {
                return (TResponse)(object)Result.Failure(error);
            }

            // N?u không ph?i Result => fallback throw exception truy?n th?ng
            throw new ValidationException(errorMessage, failures);
        }
    }
}
