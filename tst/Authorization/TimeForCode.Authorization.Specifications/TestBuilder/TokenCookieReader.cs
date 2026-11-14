using AwesomeAssertions;
using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Text.Json;
using System.Web;
using TimeForCode.Shared.Api.Authentication;
using TimeForCode.Shared.Api.Authentication.Models;

namespace TimeForCode.Authorization.Specifications.TestBuilder
{
    /// <summary>
    /// Reads the access token the API set as a cookie: the response body carries no token (ADR-005).
    /// </summary>
    internal static class TokenCookieReader
    {
        internal static JwtSecurityToken ReadAccessToken(CookieContainer cookieContainer)
        {
            var cookie = cookieContainer.GetAllCookies()[CookieConstants.TokenKey];
            cookie.Should().NotBeNull("the sign-in sets the access token as a cookie");

            var accessToken = JsonSerializer.Deserialize<AccessToken>(HttpUtility.UrlDecode(cookie!.Value))!;
            return new JwtSecurityTokenHandler().ReadJwtToken(accessToken.Token);
        }
    }
}