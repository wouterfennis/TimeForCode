using System.Net.Http.Headers;

namespace TimeForCode.Frontend.Bff.Specifications.Mocking
{
    /// <summary>
    /// Acts like a browser on the website: keeps cookies, follows no redirects by itself, and records every address it
    /// visits or is sent to and every body it receives. Cookies are kept by name, because <see cref="System.Net.CookieContainer"/>
    /// refuses to send <c>Secure</c> cookies to <c>http://localhost</c>, which browsers do.
    /// </summary>
    public sealed class Browser(HttpClient client) : IDisposable
    {
        private readonly Dictionary<string, string> _cookies = new(StringComparer.Ordinal);

        public Uri BaseAddress => client.BaseAddress!;

        public IReadOnlyDictionary<string, string> Cookies => _cookies;

        /// <summary>The raw <c>Set-Cookie</c> headers received, to inspect their attributes.</summary>
        public List<string> SetCookieHeaders { get; } = [];

        public List<string> SeenAddresses { get; } = [];

        public List<string> SeenBodies { get; } = [];

        public void SetCookie(string name, string value) => _cookies[name] = value;

        public Task<HttpResponseMessage> GetAsync(string address) => SendAsync(new HttpRequestMessage(HttpMethod.Get, address));

        public async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request)
        {
            Uri target = request.RequestUri!.IsAbsoluteUri ? request.RequestUri : new Uri(BaseAddress, request.RequestUri);
            request.RequestUri = target;
            SeenAddresses.Add(target.AbsoluteUri);
            if (_cookies.Count > 0 && IsWebsite(target))
            {
                request.Headers.Add("Cookie", string.Join("; ", _cookies.Select(cookie => $"{cookie.Key}={cookie.Value}")));
            }

            HttpResponseMessage response = await client.SendAsync(request);
            if (response.Headers.Location is not null)
            {
                SeenAddresses.Add(response.Headers.Location.OriginalString);
            }

            if (IsWebsite(target) && response.Headers.TryGetValues("Set-Cookie", out IEnumerable<string>? setCookies))
            {
                foreach (string setCookie in setCookies)
                {
                    Store(setCookie);
                }
            }

            SeenBodies.Add(await response.Content.ReadAsStringAsync());
            return response;
        }

        public void Dispose() => client.Dispose();

        private bool IsWebsite(Uri address) => Uri.Compare(address, BaseAddress, UriComponents.SchemeAndServer, UriFormat.Unescaped, StringComparison.OrdinalIgnoreCase) == 0;

        private void Store(string setCookie)
        {
            SetCookieHeaders.Add(setCookie);
            string[] parts = setCookie.Split(';', StringSplitOptions.TrimEntries);
            string[] nameValue = parts[0].Split('=', 2);
            bool expired = nameValue[1].Length == 0 || parts.Skip(1).Any(IsInThePast);
            if (expired)
            {
                _cookies.Remove(nameValue[0]);
            }
            else
            {
                _cookies[nameValue[0]] = nameValue[1];
            }
        }

        private static bool IsInThePast(string attribute)
        {
            string[] pair = attribute.Split('=', 2);
            return pair[0].ToLowerInvariant() switch
            {
                "max-age" => int.TryParse(pair[1], out int seconds) && seconds <= 0,
                "expires" => DateTimeOffset.TryParse(pair[1], out DateTimeOffset expires) && expires <= DateTimeOffset.UtcNow,
                _ => false,
            };
        }

        public static HttpRequestMessage StateChanging(HttpMethod method, string address)
        {
            var request = new HttpRequestMessage(method, address);
            request.Headers.Add("X-Requested-With", "XMLHttpRequest");
            request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            return request;
        }
    }
}