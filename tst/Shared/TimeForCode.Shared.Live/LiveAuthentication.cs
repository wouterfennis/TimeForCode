using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.RegularExpressions;
using TimeForCode.Shared.Api.Authentication.Models;

namespace TimeForCode.Shared.Live
{
    /// <summary>
    /// Obtains real access tokens from the Authorization API of the compose stack for <c>@live</c> scenarios.
    /// </summary>
    public static partial class LiveAuthentication
    {
        public const string AuthorizationUrlVariable = "TIMEFORCODE_LIVE_AUTHORIZATION_URL";
        public const string TestAdminSecretVariable = "TIMEFORCODE_LIVE_TEST_ADMIN_SECRET";
        public const string ClientRedirectUriVariable = "TIMEFORCODE_LIVE_CLIENT_REDIRECT_URI";

        private const string DefaultClientRedirectUri = "http://localhost:8084";
        private const string AccessTokenCookie = "AccessToken";

        private static readonly TimeSpan RequestTimeout = TimeSpan.FromSeconds(30);

        private static readonly JsonSerializerOptions WebJsonOptions = new(JsonSerializerDefaults.Web);

        private static readonly LiveTokenCache UserToken = new(SignInFromEnvironmentAsync);
        private static readonly LiveTokenCache AdminToken = new(RequestTestAdminTokenFromEnvironmentAsync);

        public static Task<string> GetUserTokenAsync() => UserToken.GetAsync();

        public static Task<string> GetAdminTokenAsync() => AdminToken.GetAsync();

        public static Uri ResolveAuthorizationAddress(string? configured)
        {
            if (string.IsNullOrWhiteSpace(configured))
            {
                throw new InvalidOperationException(
                    $"{AuthorizationUrlVariable} must be set to the Authorization API base URL for live scenarios that sign in.");
            }

            return new Uri(configured, UriKind.Absolute);
        }

        public static string ResolveTestAdminSecret(string? configured)
        {
            if (string.IsNullOrWhiteSpace(configured))
            {
                throw new InvalidOperationException(
                    $"{TestAdminSecretVariable} must be set to the TEST_ADMIN_ACCESS_SECRET of the stack for live scenarios that need an administrator.");
            }

            return configured;
        }

        public static Uri ResolveClientRedirectUri(string? configured)
        {
            return new Uri(string.IsNullOrWhiteSpace(configured) ? DefaultClientRedirectUri : configured, UriKind.Absolute);
        }

        public static string ExtractInputValue(string html, string name)
        {
            foreach (Match input in InputTagRegex().Matches(html))
            {
                Dictionary<string, string> attributes = AttributeRegex().Matches(input.Value)
                    .GroupBy(a => a.Groups["attribute"].Value.ToLowerInvariant())
                    .ToDictionary(g => g.Key, g => g.First().Groups["text"].Value);

                if (attributes.GetValueOrDefault("name") == name && attributes.TryGetValue("value", out string? value))
                {
                    return WebUtility.HtmlDecode(value);
                }
            }

            throw new InvalidOperationException($"The sign-in form has no input named '{name}'.");
        }

        public static string ReadAccessToken(IEnumerable<string> setCookieHeaders)
        {
            string prefix = AccessTokenCookie + "=";
            string? value = setCookieHeaders
                .Where(h => h.StartsWith(prefix, StringComparison.Ordinal))
                .Select(h => h[prefix.Length..].Split(';')[0])
                .FirstOrDefault(v => !string.IsNullOrWhiteSpace(v));

            if (value is null)
            {
                throw new InvalidOperationException($"The sign-in callback did not set an {AccessTokenCookie} cookie with a value.");
            }

            return JsonSerializer.Deserialize<AccessToken>(Uri.UnescapeDataString(value), WebJsonOptions)!.Token;
        }

        /// <summary>Logs in through the Authorization API and the mock identity provider, like a browser would.</summary>
        public static async Task<string> SignInAsync(Uri authorizationAddress, Uri clientRedirectUri)
        {
            using var handler = new HttpClientHandler { AllowAutoRedirect = false, UseCookies = true };
            using var client = new HttpClient(handler) { Timeout = RequestTimeout };

            string loginUrl = new Uri(authorizationAddress, "api/v1/authentication/login").AbsoluteUri
                + $"?IdentityProvider=Github&RedirectUri={Uri.EscapeDataString(clientRedirectUri.AbsoluteUri)}";
            Uri identityProviderUrl = await GetRedirectAsync(client, loginUrl);

            string form = await client.GetStringAsync(identityProviderUrl);
            using var confirmation = new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["state"] = ExtractInputValue(form, "state"),
                ["redirect_uri"] = ExtractInputValue(form, "redirect_uri"),
                ["client_id"] = ExtractInputValue(form, "client_id"),
                ["scope"] = ExtractInputValue(form, "scope")
            });
            using HttpResponseMessage confirmed = await client.PostAsync(new Uri(identityProviderUrl, "/login/oauth/confirm"), confirmation);
            Uri callbackUrl = RedirectTarget(confirmed, identityProviderUrl);

            using HttpResponseMessage callback = await client.GetAsync(callbackUrl);
            if (!callback.Headers.TryGetValues("Set-Cookie", out IEnumerable<string>? cookies))
            {
                throw new InvalidOperationException($"The sign-in callback answered {(int)callback.StatusCode} without setting cookies.");
            }

            return ReadAccessToken(cookies);
        }

        public static async Task<string> RequestTestAdminTokenAsync(Uri authorizationAddress, string secret)
        {
            using var client = new HttpClient { Timeout = RequestTimeout };
            using HttpResponseMessage response = await client.PostAsJsonAsync(
                new Uri(authorizationAddress, "api/v1/test-admin-access/token"), new { Secret = secret });

            if (!response.IsSuccessStatusCode)
            {
                throw new InvalidOperationException(
                    $"The stack refused the test admin token with {(int)response.StatusCode}; is TEST_ADMIN_ACCESS_SECRET set on the Authorization API?");
            }

            var token = await response.Content.ReadFromJsonAsync<AccessToken>(WebJsonOptions);
            return token!.Token;
        }

        private static Task<string> SignInFromEnvironmentAsync()
        {
            return SignInAsync(
                ResolveAuthorizationAddress(Environment.GetEnvironmentVariable(AuthorizationUrlVariable)),
                ResolveClientRedirectUri(Environment.GetEnvironmentVariable(ClientRedirectUriVariable)));
        }

        private static Task<string> RequestTestAdminTokenFromEnvironmentAsync()
        {
            return RequestTestAdminTokenAsync(
                ResolveAuthorizationAddress(Environment.GetEnvironmentVariable(AuthorizationUrlVariable)),
                ResolveTestAdminSecret(Environment.GetEnvironmentVariable(TestAdminSecretVariable)));
        }

        private static async Task<Uri> GetRedirectAsync(HttpClient client, string url)
        {
            using HttpResponseMessage response = await client.GetAsync(url);
            return RedirectTarget(response, new Uri(url));
        }

        private static Uri RedirectTarget(HttpResponseMessage response, Uri requestUri)
        {
            if ((int)response.StatusCode is < 300 or >= 400 || response.Headers.Location is null)
            {
                throw new InvalidOperationException($"Expected a redirect from {requestUri} but got {(int)response.StatusCode}.");
            }

            return new Uri(requestUri, response.Headers.Location);
        }

        [GeneratedRegex("""<input\b[^>]*>""", RegexOptions.IgnoreCase)]
        private static partial Regex InputTagRegex();

        [GeneratedRegex("""(?<![\w-])(?<attribute>name|value)\s*=\s*(?:"(?<text>[^"]*)"|'(?<text>[^']*)')""", RegexOptions.IgnoreCase)]
        private static partial Regex AttributeRegex();
    }
}