namespace ConferenceRoomBooking.Application.Common;

/// <summary>
/// Обгортка результату операції для очікуваних бізнес-помилок,
/// щоб не використовувати exceptions для керування потоком.
/// </summary>
public class Result
{
    public bool IsSuccess { get; }
    public string? Error { get; }
    public ResultErrorType ErrorType { get; }

    protected Result(bool isSuccess, string? error, ResultErrorType errorType)
    {
        IsSuccess = isSuccess;
        Error = error;
        ErrorType = errorType;
    }

    public static Result Success() => new(true, null, ResultErrorType.None);

    public static Result Failure(string error, ResultErrorType errorType = ResultErrorType.Validation) =>
        new(false, error, errorType);

    public static Result<T> Success<T>(T value) => new(value, true, null, ResultErrorType.None);

    public static Result<T> Failure<T>(string error, ResultErrorType errorType = ResultErrorType.Validation) =>
        new(default, false, error, errorType);
}

public sealed class Result<T> : Result
{
    public T? Value { get; }

    internal Result(T? value, bool isSuccess, string? error, ResultErrorType errorType)
        : base(isSuccess, error, errorType)
    {
        Value = value;
    }
}

/// <summary>
/// Тип помилки — використовується контролерами для мапінгу у правильний HTTP-статус.
/// </summary>
public enum ResultErrorType
{
    None,
    NotFound,
    Validation,
    Conflict
}