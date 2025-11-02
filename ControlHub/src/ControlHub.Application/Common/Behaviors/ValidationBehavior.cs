using FluentValidation;
using MediatR;
using ControlHub.SharedKernel.Results;
using ControlHub.SharedKernel.Common.Errors;

namespace ControlHub.Application.Common.Behaviors
{
    public class ValidationBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
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

            // Ghép lỗi thành Error chuẩn
            var errorMessage = string.Join("; ", failures.Select(f => f.ErrorMessage));
            var error = Error.Validation("Validation.Failed", errorMessage);

            // Trường hợp Handler trả về Result<T>
            if (typeof(TResponse).IsGenericType &&
                typeof(TResponse).GetGenericTypeDefinition() == typeof(Result<>))
            {
                var genericType = typeof(TResponse).GetGenericArguments()[0];
                var failureMethod = typeof(Result<>)
                    .MakeGenericType(genericType)
                    .GetMethod(nameof(Result<object>.Failure), new[] { typeof(Error) });

                if (failureMethod == null)
                    throw new InvalidOperationException($"Cannot find Failure(Error) on Result<{genericType.Name}>");

                var result = failureMethod.Invoke(null, new object[] { error });
                return (TResponse)result!;
            }

            // Trường hợp Handler trả về Result (non-generic)
            if (typeof(TResponse) == typeof(Result))
            {
                return (TResponse)(object)Result.Failure(error);
            }

            // Nếu không phải Result => fallback throw exception truyền thống
            throw new ValidationException(errorMessage, failures);
        }
    }
}