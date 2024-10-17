namespace TimeForCode.Authorization.Api.Client.Extensions
{
    public static class AuthClientExtensions
    {
        public static async Task<TryVoid<ApiException?>> TryLoginAsync(this IAuthClient client, LoginRequestModel model)
        {
            try
            {
                await client.LoginAsync(model);
            }
            catch (ApiException exception)
            {
                return TryVoid<ApiException?>.Create(exception);
            }

            return TryVoid<ApiException?>.Create(default);
        }

        public static async Task<TryResponse<CallbackResponseModel?, ApiException<ProblemDetails>?>> TryCallbackAsync(this IAuthClient client, string code, string state)
        {
            CallbackResponseModel? response = default;
            try
            {
                response = await client.CallbackAsync(code, state);
            }
            catch (ApiException<ProblemDetails> exception)
            {
                return TryResponse<CallbackResponseModel?, ApiException<ProblemDetails>?>.Create(response, exception);
            }

            return TryResponse<CallbackResponseModel?, ApiException<ProblemDetails>?>.Create(response, default);
        }
    }
}