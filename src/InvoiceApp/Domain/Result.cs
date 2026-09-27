namespace InvoiceApp.Domain;

public sealed class Result
{
    public bool IsSuccess { get; }
    public string? Error { get; }
    public IReadOnlyDictionary<string, string>? Errors { get; }

    private Result(bool isSuccess, string? error, IReadOnlyDictionary<string, string>? errors)
    {
        IsSuccess = isSuccess;
        Error = error;
        Errors = errors;
    }

    public static Result Success() => new(true, null, null);

    public static Result Failure(string error) => new(false, error, null);

    public static Result Failure(IReadOnlyDictionary<string, string> errors) =>
        new(false, null, errors);
}

public sealed class Result<T>
{
    public bool IsSuccess { get; }
    public T? Value { get; }
    public string? Error { get; }
    public IReadOnlyDictionary<string, string>? Errors { get; }

    private Result(
        bool isSuccess,
        T? value,
        string? error,
        IReadOnlyDictionary<string, string>? errors
    )
    {
        IsSuccess = isSuccess;
        Value = value;
        Error = error;
        Errors = errors;
    }

    public static Result<T> Success(T value) => new(true, value, null, null);

    public static Result<T> Failure(string error) => new(false, default, error, null);

    public static Result<T> Failure(IReadOnlyDictionary<string, string> errors) =>
        new(false, default, null, errors);
}
