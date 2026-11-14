using TimeForCode.Authorization.Values;

namespace TimeForCode.Authorization.Api.Client.Extensions
{
    public static class AuthClientExtensions
    {
        public static async Task<TryVoid<ApiException?>> TryLoginAsync(this IAuthClient client, Values.IdentityProvider identityProvider, Uri redirectUri, string codeChallenge)
        {
            try
            {
                await client.LoginAsync(Enum.Parse<Client.IdentityProvider>(identityProvider.ToString()), redirectUri, codeChallenge);
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

        /// <summary>
        /// Completes the sign-in. A success is a redirect, which the generated client reports as an <see cref="ApiException"/> with status 302.
        /// </summary>
        public static async Task<TryVoid<ApiException?>> TryCallbackAsync(this IAuthClient client, string code, string state)
        {
            try
            {
                await client.CallbackAsync(code, state);
            }
            catch (ApiException exception)
            {
                return TryVoid<ApiException?>.Create(exception);
            }

            return TryVoid<ApiException?>.Create(default);
        }

        public static async Task<TryResponse<TokenResponseModel?, ApiException?>> TryExchangeCodeAsync(this IAuthClient client, string code, string codeVerifier)
        {
            TokenResponseModel? response = default;
            try
            {
                response = await client.TokenAsync(new ExchangeCodeRequestModel { Code = code, CodeVerifier = codeVerifier });
            }
            catch (ApiException exception)
            {
                return TryResponse<TokenResponseModel?, ApiException?>.Create(response, exception);
            }

            return TryResponse<TokenResponseModel?, ApiException?>.Create(response, default);
        }

        public static async Task<TryVoid<ApiException?>> TryLogoutAsync(this IAuthClient client, RefreshToken refreshToken)
        {
            try
            {
                await client.LogoutAsync(new RefreshRequestModel { RefreshToken = refreshToken });
            }
            catch (ApiException exception)
            {
                return TryVoid<ApiException?>.Create(exception);
            }

            return TryVoid<ApiException?>.Create(default);
        }

        public static async Task<TryVoid<ApiException?>> TryDeleteAccountAsync(this IAuthClient client)
        {
            try
            {
                await client.DeleteAccountAsync();
            }
            catch (ApiException exception)
            {
                return TryVoid<ApiException?>.Create(exception);
            }

            return TryVoid<ApiException?>.Create(default);
        }

        public static async Task<TryResponse<TokenResponseModel?, ApiException<ProblemDetails>?>> TryRefreshAsync(this IAuthClient client, RefreshToken refreshToken)
        {
            TokenResponseModel? response = default;
            try
            {
                response = await client.RefreshAsync(new RefreshRequestModel { RefreshToken = refreshToken });
            }
            catch (ApiException<ProblemDetails>? exception)
            {
                return TryResponse<TokenResponseModel?, ApiException<ProblemDetails>?>.Create(response, exception);
            }

            return TryResponse<TokenResponseModel?, ApiException<ProblemDetails>?>.Create(response, default);
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