using TimeForCode.Authorization.Commands;
using TimeForCode.Authorization.Values;
using TimeForCode.Shared.Api.Authentication.Models;
using AccountInformation = TimeForCode.Authorization.Domain.Entities.AccountInformation;

namespace TimeForCode.Authorization.Application.Services
{
    public interface ITokenService
    {
        Task<Result<ExternalAccessToken>> GetAccessTokenFromExternalProviderAsync(string state, string code);

        /// <summary>
        /// Generates a user token carrying the account's GitHub login and avatar URL as the <c>login</c> and
        /// <c>avatar_url</c> claims, so resource APIs can show the user without calling back.
        /// </summary>
        AccessToken GenerateInternalToken(AccountInformation account);

        /// <summary>
        /// Generates an internal token carrying an additional role and scope claim. Used exclusively by the
        /// admin passkey flow; regular users get the account overload.
        /// </summary>
        AccessToken GenerateInternalToken(string userId, string role, string scope);
        Task<Result<AccessToken>> RefreshInternalTokenAsync(RefreshToken refreshToken);
        Uri GetRedirectUri(string state);
    }
}