using Microsoft.AspNetCore.Components.Authorization;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text.Json;
using TimeForCode.Shared.Api.Authentication;
using TimeForCode.Shared.Api.Authentication.Models;

namespace TimeForCode.Website.Components.Authentication
{
    /// <summary>
    /// Builds the Blazor <see cref="AuthenticationState"/> from the claims of the internal JWT stored in
    /// the HttpOnly "AccessToken" cookie. Role-generic — not admin-specific — so any future role reuses it.
    /// </summary>
    // TODO(review): the JWT signature is not verified here, only decoded. This matches the Website's
    // existing trust model (it already only checks cookie presence elsewhere and lets the Authorization
    // API be the real enforcement boundary). The /admin landing page has no sensitive content today, but
    // once it does, this should validate the signature against the API's JWKS endpoint before trusting the
    // role claim for anything beyond navigation/UI.
    public class JwtCookieAuthenticationStateProvider : AuthenticationStateProvider
    {
        private readonly IHttpContextAccessor _httpContextAccessor;

        public JwtCookieAuthenticationStateProvider(IHttpContextAccessor httpContextAccessor)
        {
            _httpContextAccessor = httpContextAccessor;
        }

        public override Task<AuthenticationState> GetAuthenticationStateAsync()
        {
            var principal = BuildPrincipal();
            return Task.FromResult(new AuthenticationState(principal));
        }

        private ClaimsPrincipal BuildPrincipal()
        {
            var cookie = _httpContextAccessor.HttpContext?.Request.Cookies[CookieConstants.TokenKey];
            if (string.IsNullOrEmpty(cookie))
            {
                return new ClaimsPrincipal(new ClaimsIdentity());
            }

            try
            {
                var accessToken = JsonSerializer.Deserialize<AccessToken>(cookie);
                if (string.IsNullOrEmpty(accessToken?.Token))
                {
                    return new ClaimsPrincipal(new ClaimsIdentity());
                }

                var jwt = new JwtSecurityTokenHandler().ReadJwtToken(accessToken.Token);
                var identity = new ClaimsIdentity(jwt.Claims, authenticationType: "Cookie", nameType: "sub", roleType: "role");
                return new ClaimsPrincipal(identity);
            }
            catch (Exception ex) when (ex is JsonException or ArgumentException)
            {
                return new ClaimsPrincipal(new ClaimsIdentity());
            }
        }
    }
}
