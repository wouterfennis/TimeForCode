using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text.Json;
using TimeForCode.Shared.Api.Authentication.Models;

namespace TimeForCode.Frontend.Bff.Specifications.Mocking
{
    /// <summary>
    /// Access tokens shaped like the Authorization API's, signed with the in-process host's test key,
    /// and the cookie value the Authorization API would set for them.
    /// </summary>
    internal static class TestTokens
    {
        public const string Login = "octocat";
        public const string AvatarUrl = "https://avatars.githubusercontent.com/u/583231";

        public static string User(DateTime? expires = null) => Create(
            [new Claim("scope", "user"), new Claim("login", Login), new Claim("avatar_url", AvatarUrl)],
            expires ?? DateTime.UtcNow.AddMinutes(15));

        public static string Admin() => Create(
            [new Claim("scope", "admin"), new Claim("role", "admin")],
            DateTime.UtcNow.AddMinutes(15));

        public static string CookieValue(string token) =>
            Uri.EscapeDataString(JsonSerializer.Serialize(new AccessToken { Token = token, ExpiresAfter = DateTimeOffset.UtcNow.AddMinutes(15) }));

        private static string Create(IEnumerable<Claim> claims, DateTime expires)
        {
            var token = new JwtSecurityToken(
                issuer: BffWebApplicationFactory.Issuer,
                audience: BffWebApplicationFactory.Audience,
                claims: claims.Prepend(new Claim("sub", "user-1")),
                notBefore: expires.AddMinutes(-30),
                expires: expires,
                signingCredentials: new SigningCredentials(BffWebApplicationFactory.SigningKey, SecurityAlgorithms.HmacSha256));
            return new JwtSecurityTokenHandler().WriteToken(token);
        }
    }
}