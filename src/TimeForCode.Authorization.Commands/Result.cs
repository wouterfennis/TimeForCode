namespace TimeForCode.Authorization.Commands
{
    public class Result<T>
    {
        private T? _value { get; init; }
        private string? _errorMessage { get; init; }
        public T Value => _value ?? throw new InvalidOperationException("The result does not contain any value.");
        public bool IsSuccess { get; init; }
        public bool IsFailure => !IsSuccess;
        public string ErrorMessage => _errorMessage ?? throw new InvalidOperationException("The result does not contain any error message.");

        public static Result<T> Success(T data)
        {
            return new Result<T> { _value = data, IsSuccess = true };
        }

        public static Result<T> Failure(string errorMessage)
        {
            return new Result<T> { IsSuccess = false, _errorMessage = errorMessage };
        }
    }
}