using AwesomeAssertions;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;
using System.Text.Json;
using TimeForCode.Authorization.Api.Controllers;
using TimeForCode.Authorization.Api.Models;
using TimeForCode.Authorization.Application.Options;
using TimeForCode.Authorization.Commands;
using TimeForCode.Authorization.Values;
using TimeForCode.Shared.Api.Authentication;
using TimeForCode.Shared.Api.Authentication.Models;

namespace TimeForCode.Authorization.Api.Tests.Controllers
{
    [TestClass]
    public class AuthenticationControllerTests
    {
        private const string RefreshPath = "/api/auth/v1/Authentication/session";

        private static readonly DateTimeOffset AccessExpiry = new(2030, 1, 1, 0, 0, 0, TimeSpan.Zero);

        private readonly Mock<ISender> _sender = new();

        private AuthenticationController CreateSut(string? refreshCookie = null)
        {
            var options = Options.Create(new AuthenticationOptions
            {
                Issuer = "issuer",
                Audience = "audience",
                ValidRedirectUris = ["https://app.test/"],
                RefreshCookiePath = RefreshPath
            });
            var context = new DefaultHttpContext();
            if (refreshCookie is not null)
            {
                context.Request.Headers.Cookie = $"{CookieConstants.RefreshTokenKey}={Uri.EscapeDataString(refreshCookie)}";
            }

            return new AuthenticationController(_sender.Object, options, NullLogger<AuthenticationController>.Instance)
            {
                ControllerContext = new ControllerContext { HttpContext = context }
            };
        }

        // RFC 6265 section 5.1.4: the browser sends a cookie when the request path equals the cookie path or continues it after a slash.
        private static bool BrowserSends(string cookiePath, string requestPath)
            => requestPath == cookiePath || requestPath.StartsWith(cookiePath + "/", StringComparison.Ordinal);

        private static string ValidRefreshCookie() => JsonSerializer.Serialize(new RefreshToken { Token = "refresh-secret", ExpiresAfter = AccessExpiry.AddDays(7) });

        private static Result<TokenResult> SuccessfulTokens(Uri? redirect = null, bool isNewUser = false)
            => Result<TokenResult>.Success(new TokenResult
            {
                InternalAccessToken = new AccessToken { Token = "access-secret", ExpiresAfter = AccessExpiry },
                RefreshToken = new RefreshToken { Token = "refresh-secret", ExpiresAfter = AccessExpiry.AddDays(7) },
                RedirectUri = redirect,
                IsNewUser = isNewUser
            });

        private static string[] SetCookies(AuthenticationController sut)
            => sut.HttpContext.Response.Headers.SetCookie.ToArray()!;

        [TestMethod]
        public async Task RefreshAsync_ValidRefreshCookie_ReturnsOnlyExpiryAndNewUserFlag()
        {
            _sender.Setup(s => s.Send(It.IsAny<RefreshCommand>(), It.IsAny<CancellationToken>())).ReturnsAsync(SuccessfulTokens());
            var sut = CreateSut(ValidRefreshCookie());

            var result = await sut.RefreshAsync();

            var body = result.Should().BeOfType<OkObjectResult>().Subject.Value.Should().BeOfType<CallbackResponseModel>().Subject;
            body.ExpiresAfter.Should().Be(AccessExpiry);
            JsonSerializer.Serialize(body).Should().NotContain("access-secret").And.NotContain("refresh-secret");
        }

        [TestMethod]
        public async Task RefreshAsync_ValidRefreshCookie_SetsBothTokenCookies()
        {
            _sender.Setup(s => s.Send(It.IsAny<RefreshCommand>(), It.IsAny<CancellationToken>())).ReturnsAsync(SuccessfulTokens());
            var sut = CreateSut(ValidRefreshCookie());

            await sut.RefreshAsync();

            var cookies = SetCookies(sut);
            cookies.Should().ContainSingle(c => c.StartsWith(CookieConstants.TokenKey + "="));
            cookies.Should().ContainSingle(c => c.StartsWith(CookieConstants.RefreshTokenKey + "="));
        }

        [TestMethod]
        public async Task RefreshAsync_ValidRefreshCookie_ScopesTheRefreshCookieToTheSessionPathOnly()
        {
            _sender.Setup(s => s.Send(It.IsAny<RefreshCommand>(), It.IsAny<CancellationToken>())).ReturnsAsync(SuccessfulTokens());
            var sut = CreateSut(ValidRefreshCookie());

            await sut.RefreshAsync();

            var cookies = SetCookies(sut);
            cookies.Single(c => c.StartsWith(CookieConstants.RefreshTokenKey + "=")).Should().Contain($"path={RefreshPath}");
            cookies.Single(c => c.StartsWith(CookieConstants.TokenKey + "=")).Should().Contain("path=/;");
        }

        [TestMethod]
        [DataRow("/api/auth/v1/Authentication/session/refresh", true)]
        [DataRow("/api/auth/v1/Authentication/session/logout", true)]
        [DataRow("/api/auth/v1/Authentication/login", false)]
        [DataRow("/api/auth/v1/Authentication/callback", false)]
        [DataRow("/api/auth/v1/Authentication/sessionx", false)]
        [DataRow("/api/auth/v1/User", false)]
        [DataRow("/api/donation/v1/Project", false)]
        public async Task RefreshAsync_RefreshCookiePath_IsSentByTheBrowserOnlyToRefreshAndLogout(string requestPath, bool expectedSent)
        {
            _sender.Setup(s => s.Send(It.IsAny<RefreshCommand>(), It.IsAny<CancellationToken>())).ReturnsAsync(SuccessfulTokens());
            var sut = CreateSut(ValidRefreshCookie());

            await sut.RefreshAsync();

            var cookiePath = SetCookies(sut).Single(c => c.StartsWith(CookieConstants.RefreshTokenKey + "="))
                .Split(';', StringSplitOptions.TrimEntries).Single(a => a.StartsWith("path=", StringComparison.OrdinalIgnoreCase))[5..];
            BrowserSends(cookiePath, requestPath).Should().Be(expectedSent);
        }

        [TestMethod]
        public void SessionEndpoints_Routes_LieUnderTheRefreshCookiePath()
        {
            var routes = new[] { nameof(AuthenticationController.RefreshAsync), nameof(AuthenticationController.LogoutAsync) }
                .Select(name => typeof(AuthenticationController).GetMethod(name)!.GetCustomAttributes(typeof(RouteAttribute), false).Cast<RouteAttribute>().Single().Template);

            routes.Should().OnlyContain(route => route.StartsWith("session/", StringComparison.Ordinal));
            AuthenticationOptions.DefaultRefreshCookiePath.Should().EndWith("/Authentication/session");
        }

        [TestMethod]
        public async Task RefreshAsync_MalformedRefreshCookie_ReturnsBadRequest()
        {
            var sut = CreateSut("not-json{");

            var result = await sut.RefreshAsync();

            result.Should().BeOfType<BadRequestObjectResult>();
            _sender.Verify(s => s.Send(It.IsAny<RefreshCommand>(), It.IsAny<CancellationToken>()), Times.Never);
        }

        [TestMethod]
        public async Task LogoutAsync_MalformedRefreshCookie_ReturnsNoContentAndClearsBothCookies()
        {
            var sut = CreateSut("not-json{");

            var result = await sut.LogoutAsync();

            result.Should().BeOfType<NoContentResult>();
            var cookies = SetCookies(sut);
            cookies.Should().Contain(c => c.StartsWith(CookieConstants.TokenKey + "=;"));
            cookies.Should().Contain(c => c.StartsWith(CookieConstants.RefreshTokenKey + "=;") && c.Contains($"path={RefreshPath}"));
        }

        [TestMethod]
        public async Task LogoutAsync_MalformedRefreshCookie_RevokesNothing()
        {
            var sut = CreateSut("not-json{");

            await sut.LogoutAsync();

            _sender.Verify(s => s.Send(It.Is<LogoutCommand>(c => c.RefreshToken == null), It.IsAny<CancellationToken>()), Times.Once);
        }

        [TestMethod]
        public async Task CallbackAsync_WithoutRedirect_ReturnsOnlyExpiryAndNewUserFlag()
        {
            _sender.Setup(s => s.Send(It.IsAny<CallbackCommand>(), It.IsAny<CancellationToken>())).ReturnsAsync(SuccessfulTokens(isNewUser: true));
            var sut = CreateSut();

            var result = await sut.CallbackAsync(new CallbackRequestModel { Code = "code", State = "state" });

            var body = result.Should().BeOfType<OkObjectResult>().Subject.Value.Should().BeOfType<CallbackResponseModel>().Subject;
            body.IsNewUser.Should().BeTrue();
            JsonSerializer.Serialize(body).Should().NotContain("access-secret").And.NotContain("refresh-secret");
        }
    }
}