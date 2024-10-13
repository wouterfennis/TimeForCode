namespace TimeForCode.Authorization.Api.Client.Extensions
{
    public class TryResponse<ResponseClass, ExceptionClass> where ResponseClass : class? where ExceptionClass : Exception?
    {
        public ResponseClass? Response { get; private set; }
        public ExceptionClass? Exception { get; private set; }

        private TryResponse(ResponseClass? response, ExceptionClass? exception)
        {
            Response = response;
            Exception = exception;
        }

        public static TryResponse<ResponseClass?, ExceptionClass?> Create(ResponseClass? response, ExceptionClass? exception)
        {
            return new TryResponse<ResponseClass?, ExceptionClass?>(response, exception);
        }
    }
}
