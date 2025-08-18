namespace ControlHub.SharedKernel.Results
{
    public class Result
    {
        public bool IsSuccess { get; }
        public string Error { get; }

        protected Result(bool isSuccess, string error)
        {
            IsSuccess = isSuccess;
            Error = error;
        }

        public static Result Success() => new(true, string.Empty);
        public static Result Failure(string error) => new(false, error);
    }

    public class Result<T> : Result
    {
        public T Value { get; }

        private Result(T value, bool isSuccess, string error)
            : base(isSuccess, error)
        {
            Value = value;
        }

        public static Result<T> Success(T value) => new(value, true, string.Empty);
        public static new Result<T> Failure(string error) => new(default!, false, error);
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

        public T GetValueOrDefault(T defaultValue = default!) =>
            HasValue ? _value! : defaultValue;
    }
}
