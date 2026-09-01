using TimeForCode.Authorization.Values;

namespace TimeForCode.Authorization.Api.Client.Extensions
{
    public static class AuthClientExtensions
    {
        public static async Task<TryVoid<ApiException?>> TryLoginAsync(this IAuthClient client, IdentityProvider identityProvider, Uri redirectUri)
        {
            try
            {
                await client.LoginAsync((int)identityProvider, redirectUri);
            }
            catch (ApiException<ProblemDetails> exception)
            {
                return TryVoid<ApiException?>.Create(exception);
            }
            catch (ApiException exception)
            {
                return TryVoid<ApiException?>.Create(exception);
            }

            return TryVoid<ApiException?>.Create(default);
        }

        public static async Task<TryResponse<CallbackResponseModel?, System.Exception?>> TryCallbackAsync(this IAuthClient client, string code, string state)
        {
            CallbackResponseModel? response = default;
            try
            {
                response = await client.CallbackAsync(code, state);
            }
            catch (ApiException<ProblemDetails> exception)
            {
                return TryResponse<CallbackResponseModel?, System.Exception?>.Create(response, exception);
            }
            catch (ApiException exception)
            {
                return TryResponse<CallbackResponseModel?, System.Exception?>.Create(response, exception);
            }

            return TryResponse<CallbackResponseModel?, System.Exception?>.Create(response, default);
        }

        public static async Task<TryVoid<ApiException?>> TryLogoutAsync(this IAuthClient client)
        {
            try
            {
                await client.LogoutAsync();
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

        public static async Task<TryResponse<ICollection<RepositoryResponse>?, ApiException?>> TryUserRepositoriesAsync(this IAuthClient client)
        {
            ICollection<RepositoryResponse>? response = default;
            try
            {
                response = await client.RepositoriesAllAsync();
            }
            catch (ApiException<ProblemDetails> exception)
            {
                return TryResponse<ICollection<RepositoryResponse>?, ApiException?>.Create(response, exception);
            }
            catch (ApiException exception)
            {
                return TryResponse<ICollection<RepositoryResponse>?, ApiException?>.Create(response, exception);
            }

            return TryResponse<ICollection<RepositoryResponse>?, ApiException?>.Create(response, default);
        }

        public static async Task<TryResponse<AdminPasskeyOptionsResponseModel?, ApiException?>> TryAdminRegistrationOptionsAsync(this IAuthClient client, string bootstrapSecret)
        {
            AdminPasskeyOptionsResponseModel? response = default;
            try
            {
                response = await client.AdminRegistrationOptionsAsync(new AdminRegistrationOptionsRequestModel { BootstrapSecret = bootstrapSecret });
            }
            catch (ApiException<ProblemDetails> exception)
            {
                return TryResponse<AdminPasskeyOptionsResponseModel?, ApiException?>.Create(response, exception);
            }
            catch (ApiException exception)
            {
                return TryResponse<AdminPasskeyOptionsResponseModel?, ApiException?>.Create(response, exception);
            }

            return TryResponse<AdminPasskeyOptionsResponseModel?, ApiException?>.Create(response, default);
        }

        public static async Task<TryResponse<CallbackResponseModel?, ApiException?>> TryAdminCompleteRegistrationAsync(this IAuthClient client, string bootstrapSecret, string credentialJson)
        {
            CallbackResponseModel? response = default;
            try
            {
                response = await client.AdminCompleteRegistrationAsync(new AdminCompleteRegistrationRequestModel { BootstrapSecret = bootstrapSecret, CredentialJson = credentialJson });
            }
            catch (ApiException<ProblemDetails> exception)
            {
                return TryResponse<CallbackResponseModel?, ApiException?>.Create(response, exception);
            }
            catch (ApiException exception)
            {
                return TryResponse<CallbackResponseModel?, ApiException?>.Create(response, exception);
            }

            return TryResponse<CallbackResponseModel?, ApiException?>.Create(response, default);
        }

        public static async Task<TryResponse<AdminPasskeyOptionsResponseModel?, ApiException?>> TryAdminAuthenticationOptionsAsync(this IAuthClient client)
        {
            AdminPasskeyOptionsResponseModel? response = default;
            try
            {
                response = await client.AdminAuthenticationOptionsAsync();
            }
            catch (ApiException<ProblemDetails> exception)
            {
                return TryResponse<AdminPasskeyOptionsResponseModel?, ApiException?>.Create(response, exception);
            }
            catch (ApiException exception)
            {
                return TryResponse<AdminPasskeyOptionsResponseModel?, ApiException?>.Create(response, exception);
            }

            return TryResponse<AdminPasskeyOptionsResponseModel?, ApiException?>.Create(response, default);
        }

        public static async Task<TryResponse<CallbackResponseModel?, ApiException?>> TryAdminCompleteAuthenticationAsync(this IAuthClient client, string credentialJson)
        {
            CallbackResponseModel? response = default;
            try
            {
                response = await client.AdminCompleteAuthenticationAsync(new AdminCompleteAuthenticationRequestModel { CredentialJson = credentialJson });
            }
            catch (ApiException<ProblemDetails> exception)
            {
                return TryResponse<CallbackResponseModel?, ApiException?>.Create(response, exception);
            }
            catch (ApiException exception)
            {
                return TryResponse<CallbackResponseModel?, ApiException?>.Create(response, exception);
            }

            return TryResponse<CallbackResponseModel?, ApiException?>.Create(response, default);
        }
    }
}