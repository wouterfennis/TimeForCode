using Microsoft.AspNetCore.Http;
using System.Net.Http.Headers;
using TimeForCode.Shared.Api.Authentication;

namespace TimeForCode.Shared.Api.Handlers
{
    /// <summary>
    /// Adds the access token from the incoming request's cookie as a bearer token to outgoing requests.
    /// </summary>
    /// <param name="httpContextAccessor">Gives access to the incoming request.</param>
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
            string? token = AccessTokenCookie.TryReadToken(_httpContextAccessor.HttpContext?.Request.Cookies[CookieConstants.TokenKey]);
            if (token is not null)
            {
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            }
            return base.SendAsync(request, cancellationToken);
        }
    }
}