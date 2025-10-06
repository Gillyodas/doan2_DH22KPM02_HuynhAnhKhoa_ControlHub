using FluentValidation;
using MediatR;
using ControlHub.SharedKernel.Results;
using ControlHub.SharedKernel.Common.Errors;

namespace ControlHub.Application.Common.Behaviors
{
    public class ValidationBehavior<TRequest, TResponse>
        : IPipelineBehavior<TRequest, TResponse>
        where TRequest : notnull
    {
        private readonly IEnumerable<IValidator<TRequest>> _validators;

        public ValidationBehavior(IEnumerable<IValidator<TRequest>> validators)
        {
            _validators = validators ?? Array.Empty<IValidator<TRequest>>();
        }

        public async Task<TResponse> Handle(
            TRequest request,
            RequestHandlerDelegate<TResponse> next,
            CancellationToken cancellationToken)
        {
            var count = _validators.Count();
            Console.WriteLine($"[ValidationBehavior] Loaded validators: {count} for {typeof(TRequest).Name}");

            if (_validators.Any())
            {
                var context = new ValidationContext<TRequest>(request);
                var failures = (await Task.WhenAll(
                        _validators.Select(v => v.ValidateAsync(context, cancellationToken))))
                    .SelectMany(r => r.Errors)
                    .Where(f => f != null)
                    .ToList();

                if (failures.Count != 0)
                {
                    var errorMessage = string.Join("; ", failures.Select(f => f.ErrorMessage));
                    var error = new Error("Validation.Failed", errorMessage);

                    // Nếu handler return Result<T>
                    if (typeof(TResponse).IsGenericType &&
                        typeof(TResponse).GetGenericTypeDefinition() == typeof(Result<>))
                    {
                        var genericArg = typeof(TResponse).GetGenericArguments()[0];
                        var method = typeof(Result<>)
                                    .MakeGenericType(genericArg)
                                    .GetMethod("Failure", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);

                        if (method == null)
                            throw new InvalidOperationException($"Cannot find Failure method on Result<{genericArg.Name}>");

                        return (TResponse)method.Invoke(null, new object[] { errorMessage, null })!;
                    }

                    // Nếu handler return Result (non-generic)
                    if (typeof(TResponse) == typeof(Result))
                    {
                        return (TResponse)(object)Result.Failure(error);
                    }

                    // Nếu không phải Result => fallback throw như cũ
                    throw new ValidationException(errorMessage, failures);
                }
            }

            return await next();
        }
    }
}