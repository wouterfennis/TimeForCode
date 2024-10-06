public class Result<T>
{
    public T? Data { get; init; }
    public bool IsSuccess { get; init; }
    public bool IsFailure => !IsSuccess;
    public string? ErrorMessage { get; init; }

    public static Result<T> Success(T data)
    {
        return new Result<T> { Data = data, IsSuccess = true };
    }

    public static Result<T> Failure(string errorMessage)
    {
        return new Result<T> { IsSuccess = false, ErrorMessage = errorMessage };
    }
}