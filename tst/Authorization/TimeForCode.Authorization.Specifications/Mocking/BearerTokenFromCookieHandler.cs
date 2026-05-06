using System.Net;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Web;
using TimeForCode.Shared.Api.Authentication;
using TimeForCode.Shared.Api.Authentication.Models;

namespace TimeForCode.Authorization.Specifications.Mocking
{
    internal class BearerTokenFromCookieHandler : DelegatingHandler
    {
        private readonly CookieContainer _cookieContainer;

        public BearerTokenFromCookieHandler(CookieContainer cookieContainer)
        {
            _cookieContainer = cookieContainer;
        }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var tokenCookie = _cookieContainer.GetAllCookies()[CookieConstants.TokenKey];
            if (tokenCookie != null)
            {
                try
                {
                    var cookieValue = HttpUtility.UrlDecode(tokenCookie.Value);
                    var accessToken = JsonSerializer.Deserialize<AccessToken>(cookieValue);
                    if (!string.IsNullOrEmpty(accessToken?.Token))
                    {
                        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken.Token);
                    }
                }
                catch (JsonException)
                {
                    // Cookie value is not valid JSON; skip setting the Authorization header.
                }
            }

            return base.SendAsync(request, cancellationToken);
        }
    }
}