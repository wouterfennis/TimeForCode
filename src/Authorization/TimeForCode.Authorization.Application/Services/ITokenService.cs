using TimeForCode.Authorization.Commands;
using TimeForCode.Authorization.Values;
using TimeForCode.Shared.Api.Authentication.Models;

namespace TimeForCode.Authorization.Application.Services
{
    public interface ITokenService
    {
        Task<Result<ExternalAccessToken>> GetAccessTokenFromExternalProviderAsync(string state, string code);
        AccessToken GenerateInternalToken(string userId);

        /// <summary>
        /// Generates an internal token carrying an additional role and scope claim. Used exclusively by the
        /// admin passkey flow; every existing caller keeps using the single-argument overload unchanged.
        /// </summary>
        AccessToken GenerateInternalToken(string userId, string role, string scope);
        Task<Result<AccessToken>> RefreshInternalTokenAsync(RefreshToken refreshToken);
        Uri GetRedirectUri(string state);
    }
}