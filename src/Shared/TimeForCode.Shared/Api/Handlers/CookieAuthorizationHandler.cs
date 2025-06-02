using Microsoft.AspNetCore.Http;
using System.Net.Http.Headers;
using System.Text.Json;
using TimeForCode.Shared.Api.Authentication;
using TimeForCode.Shared.Api.Authentication.Models;

namespace TimeForCode.Shared.Api.Handlers
{
    public class CookieAuthorizationHandler : DelegatingHandler
    {
        private readonly IHttpContextAccessor _httpContextAccessor;

        public CookieAuthorizationHandler(IHttpContextAccessor httpContextAccessor)
        {
            _httpContextAccessor = httpContextAccessor;
        }

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