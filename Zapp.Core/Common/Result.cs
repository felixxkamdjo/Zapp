namespace Zapp.Core.Common;

// encapsulate functional operation outcome without throwing exceptions
public readonly struct Result<T>
{
    public bool IsSuccess { get; }
    public T? Value { get; }
    public string? ErrorMessage { get; }

    private Result(bool isSuccess, T? value, string? errorMessage)
    {
        IsSuccess = isSuccess;
        Value = value;
        ErrorMessage = errorMessage;
    }

    // construct successful operation result
    public static Result<T> Success(T value) => new(true, value, null);

    // construct failed operation result
    public static Result<T> Failure(string errorMessage) => new(false, default, errorMessage);
}