using System.Net;

namespace TimeForCode.Reporting.Commands
{
    public class Result<T>
    {
        internal const string MissingErrorMessage = "The result does not contain any error message.";

        private T? _value { get; init; }
        private string? _errorMessage { get; init; }
        public T Value => _value!;
        public bool IsSuccess { get; init; }
        public bool IsFailure => !IsSuccess;
        public string ErrorMessage => _errorMessage!;
        public HttpStatusCode? FailureStatusCode { get; init; }

        public static Result<T> Success(T data)
        {
            if (data is null)
            {
                throw new InvalidOperationException("The result does not contain any value.");
            }

            return new Result<T> { _value = data, IsSuccess = true };
        }

        public static Result<T> Failure(string? errorMessage, HttpStatusCode? statusCode = null)
        {
            if (string.IsNullOrEmpty(errorMessage))
            {
                throw new InvalidOperationException(MissingErrorMessage);
            }

            return new Result<T> { IsSuccess = false, _errorMessage = errorMessage, FailureStatusCode = statusCode };
        }
    }
}