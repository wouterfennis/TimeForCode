using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Moq;
using System.Net;
using System.Text.Json;
using TimeForCode.Shared.Api.Authentication;
using TimeForCode.Shared.Api.Authentication.Models;
using TimeForCode.Shared.Api.Handlers;

namespace TimeForCode.Shared.Tests.Api.Handlers
{
    [TestClass]
    public class CookieAuthorizationHandlerTests
    {
        private readonly Mock<IHttpContextAccessor> _httpContextAccessorMock;
        private readonly TestHttpMessageHandler _innerHandler;
        private readonly HttpClient _client;

        public CookieAuthorizationHandlerTests()
        {
            _httpContextAccessorMock = new Mock<IHttpContextAccessor>();
            _innerHandler = new TestHttpMessageHandler();
            var handler = new CookieAuthorizationHandler(_httpContextAccessorMock.Object)
            {
                InnerHandler = _innerHandler
            };
            _client = new HttpClient(handler);
        }

        [TestMethod]
        public async Task SendAsync_WithValidTokenCookie_AddsAuthorizationHeader()
        {
            var accessToken = new AccessToken { Token = "valid-access-token", ExpiresAfter = DateTimeOffset.UtcNow.AddHours(1) };
            SetupCookieWithToken(accessToken);

            await _client.GetAsync("http://example.com/api/test");

            _innerHandler.LastRequest!.Headers.Authorization.Should().NotBeNull();
            _innerHandler.LastRequest.Headers.Authorization!.Scheme.Should().Be("Bearer");
            _innerHandler.LastRequest.Headers.Authorization.Parameter.Should().Be("valid-access-token");
        }

        [TestMethod]
        public async Task SendAsync_WithNullHttpContext_DoesNotAddAuthorizationHeader()
        {
            _httpContextAccessorMock.Setup(a => a.HttpContext).Returns((HttpContext?)null);

            await _client.GetAsync("http://example.com/api/test");

            _innerHandler.LastRequest!.Headers.Authorization.Should().BeNull();
        }

        [TestMethod]
        public async Task SendAsync_WithMissingTokenCookie_DoesNotAddAuthorizationHeader()
        {
            SetupCookieWithToken(null);

            await _client.GetAsync("http://example.com/api/test");

            _innerHandler.LastRequest!.Headers.Authorization.Should().BeNull();
        }

        [TestMethod]
        public async Task SendAsync_WithEmptyTokenCookie_DoesNotAddAuthorizationHeader()
        {
            SetupCookieValue(string.Empty);

            await _client.GetAsync("http://example.com/api/test");

            _innerHandler.LastRequest!.Headers.Authorization.Should().BeNull();
        }

        [TestMethod]
        public async Task SendAsync_WithValidToken_CallsInnerHandler()
        {
            var accessToken = new AccessToken { Token = "some-token", ExpiresAfter = DateTimeOffset.UtcNow.AddHours(1) };
            SetupCookieWithToken(accessToken);

            var response = await _client.GetAsync("http://example.com/api/test");

            response.StatusCode.Should().Be(HttpStatusCode.OK);
            _innerHandler.CallCount.Should().Be(1);
        }

        [TestMethod]
        public async Task SendAsync_WithoutToken_StillCallsInnerHandler()
        {
            _httpContextAccessorMock.Setup(a => a.HttpContext).Returns((HttpContext?)null);

            var response = await _client.GetAsync("http://example.com/api/test");

            response.StatusCode.Should().Be(HttpStatusCode.OK);
            _innerHandler.CallCount.Should().Be(1);
        }

        private void SetupCookieWithToken(AccessToken? accessToken)
        {
            var cookieValue = accessToken is not null ? JsonSerializer.Serialize(accessToken) : null;
            SetupCookieValue(cookieValue);
        }

        private void SetupCookieValue(string? cookieValue)
        {
            var cookiesMock = new Mock<IRequestCookieCollection>();
            cookiesMock.Setup(c => c[CookieConstants.TokenKey]).Returns(cookieValue);

            var httpRequest = new Mock<HttpRequest>();
            httpRequest.Setup(r => r.Cookies).Returns(cookiesMock.Object);

            var httpContext = new Mock<HttpContext>();
            httpContext.Setup(c => c.Request).Returns(httpRequest.Object);

            _httpContextAccessorMock.Setup(a => a.HttpContext).Returns(httpContext.Object);
        }

        private sealed class TestHttpMessageHandler : HttpMessageHandler
        {
            public HttpRequestMessage? LastRequest { get; private set; }
            public int CallCount { get; private set; }

            protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            {
                LastRequest = request;
                CallCount++;
                return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK));
            }
        }
    }
}
