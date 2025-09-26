using ControlHub.SharedKernel.Common.Errors;

namespace ControlHub.SharedKernel.Results
{
    public class Result
    {
        public bool IsSuccess { get; }
        public bool IsFailure => !IsSuccess;
        public Error Error { get; }
        public Exception? Exception { get; }

        protected Result(bool isSuccess, Error error, Exception? exception = null)
        {
            IsSuccess = isSuccess;
            Error = error;
            Exception = exception;
        }

        public static Result Success() =>
            new(true, Error.None);

        public static Result Failure(Error error, Exception? ex = null) =>
            new(false, error, ex);
    }

    public class Result<T> : Result
    {
        public T Value { get; }

        private Result(T value, bool isSuccess, Error error, Exception? exception = null)
            : base(isSuccess, error, exception)
        {
            Value = value;
        }

        public static Result<T> Success(T value) =>
            new(value, true, Error.None);

        public static Result<T> Failure(Error error, Exception? ex = null) =>
            new(default!, false, error, ex);
    }

    public class Maybe<T>
    {
        private readonly T? _value;

        public bool HasValue { get; }
        public bool HasNoValue => !HasValue;
        public T Value => HasValue
            ? _value!
            : throw new InvalidOperationException("No value present");

        private Maybe() => HasValue = false;

        private Maybe(T value)
        {
            _value = value;
            HasValue = true;
        }

        public static Maybe<T> None => new Maybe<T>();

        public static Maybe<T> From(T value) =>
            value == null ? None : new Maybe<T>(value);

        public TResult Match<TResult>(
            Func<T, TResult> some,
            Func<TResult> none) =>
            HasValue ? some(_value!) : none();

        public void Match(Action<T> some, Action none)
        {
            if (HasValue) some(_value!);
            else none();
        }

        public T GetValueOrDefault(T defaultValue = default!) =>
            HasValue ? _value! : defaultValue;
    }
}