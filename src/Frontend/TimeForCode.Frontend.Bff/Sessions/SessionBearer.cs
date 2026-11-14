using System.Net.Http.Headers;
using Yarp.ReverseProxy.Transforms;
using Yarp.ReverseProxy.Transforms.Builder;

namespace TimeForCode.Frontend.Bff.Sessions
{
    /// <summary>
    /// For every forwarded call: drops what the browser sent (<c>Cookie</c>, <c>Authorization</c>) and, when the cookie maps to a
    /// session, adds the bearer header with a valid access token, refreshed first when it expired. The APIs only ever see bearer
    /// tokens; the browser never sees one.
    /// </summary>
    internal static class SessionBearer
    {
        public static void Add(TransformBuilderContext context) =>
            context.AddRequestTransform(async transform =>
            {
                var headers = transform.ProxyRequest.Headers;
                headers.Remove("Cookie");
                headers.Remove("Authorization");

                var http = transform.HttpContext;
                var sessions = http.RequestServices.GetRequiredService<SessionService>();
                var session = await BrowserSession.ResolveAsync(http, sessions);
                if (session is null)
                {
                    return;
                }

                var accessToken = await sessions.GetAccessTokenAsync(session, http.RequestAborted);
                if (accessToken is null)
                {
                    // The refresh token was rejected or the Authorization API is unreachable: the call goes out anonymous.
                    BrowserSession.ClearSessionCookieWhenTheResponseStarts(http);
                    return;
                }

                headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
            });
    }
}