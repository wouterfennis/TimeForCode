using Microsoft.AspNetCore.Http;
using System.Net.Http.Headers;
using System.Text.Json;
using TimeForCode.Shared.Api.Authentication;
using TimeForCode.Shared.Api.Authentication.Models;

namespace TimeForCode.Shared.Api.Handlers
{
    public class CookieAuthorizationHandler(IHttpContextAccessor httpContextAccessor) : DelegatingHandler
    {
        private readonly IHttpContextAccessor _httpContextAccessor = httpContextAccessor;

        /// <summary>
        /// Sends an HTTP request with the access token from the cookie if it exists.
        /// </summary>
        /// <param name="request">The HTTP request message.</param>
        /// <param name="cancellationToken">A cancellation token to cancel the operation.</param>
        /// <returns>A task that represents the asynchronous operation.</returns>
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var token = _httpContextAccessor.HttpContext?.Request.Cookies[CookieConstants.TokenKey];
            if (!string.IsNullOrEmpty(token))
            {
                var accessToken = JsonSerializer.Deserialize<AccessToken>(token);
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken!.Token);
            }
            return base.SendAsync(request, cancellationToken);
        }
    }
}