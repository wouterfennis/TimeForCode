using Microsoft.AspNetCore.Http;
using TimeForCode.Shared.Api.Authentication;

namespace TimeForCode.Shared.Api.Handlers
{
    public class RefreshTokenForwardingHandler(IHttpContextAccessor httpContextAccessor) : DelegatingHandler
    {
        private readonly IHttpContextAccessor _httpContextAccessor = httpContextAccessor;

        /// <summary>
        /// Forwards the refresh token cookie from the incoming browser request to the outgoing auth API request.
        /// </summary>
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var cookieValue = _httpContextAccessor.HttpContext?.Request.Cookies[CookieConstants.RefreshTokenKey];
            if (!string.IsNullOrEmpty(cookieValue))
            {
                request.Headers.TryAddWithoutValidation("Cookie", $"{CookieConstants.RefreshTokenKey}={cookieValue}");
            }

            return base.SendAsync(request, cancellationToken);
        }
    }
}