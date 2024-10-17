namespace TimeForCode.Authorization.Api.Client.Extensions
{
    public class TryVoid<ExceptionClass> where ExceptionClass : Exception?
    {
        public ExceptionClass? Exception { get; private set; }

        private TryVoid(ExceptionClass? exception)
        {
            Exception = exception;
        }

        public static TryVoid<ExceptionClass?> Create(ExceptionClass? exception)
        {
            return new TryVoid<ExceptionClass?>(exception);
        }
    }
}