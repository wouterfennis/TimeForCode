namespace TimeForCode.Authorization.Api.Client.Extensions
{
    public static class AuthClientExtensions
    {
        public static async Task<TryVoid<ApiException?>> TryLoginAsync(this IAuthClient client, IdentityProvider idenityProvider, Uri redirectUri)
        {
            try
            {
                await client.LoginAsync(idenityProvider, redirectUri);
            }
            catch (ApiException exception)
            {
                return TryVoid<ApiException?>.Create(exception);
            }

            return TryVoid<ApiException?>.Create(default);
        }

        public static async Task<TryResponse<CallbackResponseModel?, Exception?>> TryCallbackAsync(this IAuthClient client, string code, string state)
        {
            CallbackResponseModel? response = default;
            try
            {
                response = await client.CallbackAsync(code, state);
            }
            catch (ApiException<ProblemDetails> exception)
            {
                return TryResponse<CallbackResponseModel?, Exception?>.Create(response, exception);
            }
            catch (ApiException exception)
            {
                return TryResponse<CallbackResponseModel?, Exception?>.Create(response, exception);
            }

            return TryResponse<CallbackResponseModel?, Exception?>.Create(response, default);
        }

        public static async Task<TryVoid<ApiException?>> TryLogoutAsync(this IAuthClient client, string redirectUri)
        {
            try
            {
                await client.LogoutAsync(redirectUri);
            }
            catch (ApiException exception)
            {
                return TryVoid<ApiException?>.Create(exception);
            }

            return TryVoid<ApiException?>.Create(default);
        }

        public static async Task<TryVoid<ApiException<ProblemDetails>?>> TryRefreshAsync(this IAuthClient client)
        {
            try
            {
                await client.RefreshAsync();
            }
            catch (ApiException<ProblemDetails>? exception)
            {
                return TryVoid<ApiException<ProblemDetails>?>.Create(exception);
            }

            return TryVoid<ApiException<ProblemDetails>?>.Create(default);
        }
    }
}