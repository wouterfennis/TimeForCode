using AwesomeAssertions;
using IdentityProviderMockService.Controllers;
using Microsoft.AspNetCore.Mvc.Testing;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace IdentityProviderMockService.Tests
{
    /// <summary>Drives the mock like a browser: authorize page, confirm form, token exchange, profile.</summary>
    internal sealed class MockSignIn : IDisposable
    {
        private const string RedirectUri = "http://localhost/callback";
        // Program is static and cannot be a type argument, so any public type of the assembly marks it.
        private readonly WebApplicationFactory<LoginController> _factory = new();
        private readonly HttpClient _client;

        public MockSignIn()
        {
            _client = _factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        }

        public async Task<string> GetAuthorizePageAsync(string? persona)
        {
            using HttpResponseMessage response = await _client.GetAsync(AuthorizeUrl(persona));
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadAsStringAsync();
        }

        public async Task<HttpStatusCode> GetAuthorizeStatusAsync(string? persona)
        {
            using HttpResponseMessage response = await _client.GetAsync(AuthorizeUrl(persona));
            return response.StatusCode;
        }

        private static string AuthorizeUrl(string? persona)
        {
            string personaQuery = persona is null ? string.Empty : $"&persona={Uri.EscapeDataString(persona)}";
            return $"/login/oauth/authorize?state=s&redirect_uri={Uri.EscapeDataString(RedirectUri)}&client_id=c&scope=user{personaQuery}";
        }

        public async Task<JsonElement> SignInAsync(string? persona)
        {
            string page = await GetAuthorizePageAsync(persona);
            var form = new Dictionary<string, string>
            {
                ["state"] = "s",
                ["redirect_uri"] = RedirectUri,
                ["client_id"] = "c",
                ["scope"] = "user",
            };
            string? hiddenPersona = HiddenInput(page, "persona");
            if (hiddenPersona is not null)
            {
                form["persona"] = hiddenPersona;
            }

            using HttpResponseMessage confirmed = await _client.PostAsync("/login/oauth/confirm", new FormUrlEncodedContent(form));
            confirmed.StatusCode.Should().Be(HttpStatusCode.Redirect);
            string code = Regex.Match(confirmed.Headers.Location!.ToString(), "code=([^&]+)").Groups[1].Value;

            using HttpResponseMessage tokenResponse = await _client.PostAsJsonAsync("/login/oauth/access_token", new
            {
                client_id = "test-client",
                client_secret = "test-secret",
                code,
                redirect_uri = RedirectUri,
            });
            tokenResponse.StatusCode.Should().Be(HttpStatusCode.OK);
            string accessToken = (await tokenResponse.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("access_token").GetString()!;

            using var request = new HttpRequestMessage(HttpMethod.Get, "/user");
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
            using HttpResponseMessage user = await _client.SendAsync(request);
            user.StatusCode.Should().Be(HttpStatusCode.OK);
            return await user.Content.ReadFromJsonAsync<JsonElement>();
        }

        public void Dispose()
        {
            _client.Dispose();
            _factory.Dispose();
        }

        private static string? HiddenInput(string html, string name)
        {
            Match match = Regex.Match(html, $"""<input type="hidden" name="{name}" value="([^"]*)" />""");
            return match.Success ? WebUtility.HtmlDecode(match.Groups[1].Value) : null;
        }
    }
}