using AwesomeAssertions;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;
using TimeForCode.Authorization.Api.Controllers;
using TimeForCode.Authorization.Api.Models;
using TimeForCode.Authorization.Application.Options;
using TimeForCode.Authorization.Commands;
using TimeForCode.Authorization.Values;
using TimeForCode.Shared.Api.Authentication.Models;

namespace TimeForCode.Authorization.Api.Tests.Controllers
{
    [TestClass]
    public class AuthenticationControllerTests
    {
        private const string Challenge = "E9Melhoa2OwvFrEMTJguCHaoeK1t8URWbuGJSstw-cM";
        private static readonly DateTimeOffset AccessExpiry = new(2030, 1, 1, 0, 0, 0, TimeSpan.Zero);

        private readonly Mock<ISender> _sender = new();

        private AuthenticationController CreateSut()
        {
            var options = Options.Create(new AuthenticationOptions
            {
                Issuer = "issuer",
                Audience = "audience",
                ValidRedirectUris = ["https://app.test/signed-in"]
            });

            return new AuthenticationController(_sender.Object, options, NullLogger<AuthenticationController>.Instance)
            {
                ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() }
            };
        }

        private static Result<TokenResult> SuccessfulTokens(bool isNewUser = false)
            => Result<TokenResult>.Success(new TokenResult
            {
                InternalAccessToken = new AccessToken { Token = "access-secret", ExpiresAfter = AccessExpiry },
                RefreshToken = new RefreshToken { Token = "refresh-secret", ExpiresAfter = AccessExpiry.AddDays(7) },
                IsNewUser = isNewUser
            });

        private static void NoCookiesShouldBeSet(AuthenticationController sut)
            => sut.HttpContext.Response.Headers.SetCookie.Should().BeEmpty();

        [TestMethod]
        public async Task LoginAsync_ValidRequest_PassesTheClientChallengeOnAndRedirects()
        {
            _sender.Setup(s => s.Send(It.IsAny<LoginCommand>(), It.IsAny<CancellationToken>())).ReturnsAsync(new Uri("https://idp.test/authorize"));
            var sut = CreateSut();

            var result = await sut.LoginAsync(new LoginRequestModel
            {
                IdentityProvider = Api.Models.IdentityProvider.Github,
                RedirectUri = new Uri("https://app.test/signed-in"),
                CodeChallenge = Challenge
            });

            result.Should().BeOfType<RedirectResult>().Which.Url.Should().Be("https://idp.test/authorize");
            _sender.Verify(s => s.Send(It.Is<LoginCommand>(c => c.CodeChallenge == Challenge), It.IsAny<CancellationToken>()), Times.Once);
        }

        [TestMethod]
        public async Task LoginAsync_UnlistedRedirectUri_ReturnsBadRequest()
        {
            var sut = CreateSut();

            var result = await sut.LoginAsync(new LoginRequestModel
            {
                IdentityProvider = Api.Models.IdentityProvider.Github,
                RedirectUri = new Uri("https://evil.test/"),
                CodeChallenge = Challenge
            });

            result.Should().BeOfType<BadRequestObjectResult>();
        }

        [TestMethod]
        public async Task CallbackAsync_Success_RedirectsToTheClientWithTheOneTimeCodeAndSetsNoCookie()
        {
            _sender.Setup(s => s.Send(It.IsAny<CallbackCommand>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(Result<CallbackResult>.Success(new CallbackResult { RedirectUri = new Uri("https://app.test/signed-in"), Code = "one-time" }));
            var sut = CreateSut();

            var result = await sut.CallbackAsync(new CallbackRequestModel { Code = "code", State = "state" });

            result.Should().BeOfType<RedirectResult>().Which.Url.Should().Be("https://app.test/signed-in?code=one-time");
            NoCookiesShouldBeSet(sut);
        }

        [TestMethod]
        public async Task CallbackAsync_Failure_ReturnsBadRequest()
        {
            _sender.Setup(s => s.Send(It.IsAny<CallbackCommand>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(Result<CallbackResult>.Failure("State is not known"));
            var sut = CreateSut();

            var result = await sut.CallbackAsync(new CallbackRequestModel { Code = "code", State = "state" });

            result.Should().BeOfType<BadRequestObjectResult>();
        }

        [TestMethod]
        public async Task ExchangeCodeAsync_ValidCode_ReturnsBothTokensAndSetsNoCookie()
        {
            _sender.Setup(s => s.Send(It.IsAny<ExchangeCodeCommand>(), It.IsAny<CancellationToken>())).ReturnsAsync(SuccessfulTokens(isNewUser: true));
            var sut = CreateSut();

            var result = await sut.ExchangeCodeAsync(new ExchangeCodeRequestModel { Code = "one-time", CodeVerifier = new string('a', 43) });

            var body = result.Should().BeOfType<OkObjectResult>().Subject.Value.Should().BeOfType<TokenResponseModel>().Subject;
            body.AccessToken.Token.Should().Be("access-secret");
            body.RefreshToken.Token.Should().Be("refresh-secret");
            body.IsNewUser.Should().BeTrue();
            NoCookiesShouldBeSet(sut);
        }

        [TestMethod]
        public async Task ExchangeCodeAsync_InvalidCode_ReturnsBadRequest()
        {
            _sender.Setup(s => s.Send(It.IsAny<ExchangeCodeCommand>(), It.IsAny<CancellationToken>())).ReturnsAsync(Result<TokenResult>.Failure("Code is not valid"));
            var sut = CreateSut();

            var result = await sut.ExchangeCodeAsync(new ExchangeCodeRequestModel { Code = "one-time", CodeVerifier = new string('a', 43) });

            result.Should().BeOfType<BadRequestObjectResult>();
        }

        [TestMethod]
        public async Task RefreshAsync_ValidRefreshToken_ReturnsNewTokensAndSetsNoCookie()
        {
            _sender.Setup(s => s.Send(It.IsAny<RefreshCommand>(), It.IsAny<CancellationToken>())).ReturnsAsync(SuccessfulTokens());
            var sut = CreateSut();

            var result = await sut.RefreshAsync(new RefreshRequestModel { RefreshToken = new RefreshToken { Token = "old", ExpiresAfter = AccessExpiry } });

            var body = result.Should().BeOfType<OkObjectResult>().Subject.Value.Should().BeOfType<TokenResponseModel>().Subject;
            body.RefreshToken.Token.Should().Be("refresh-secret");
            _sender.Verify(s => s.Send(It.Is<RefreshCommand>(c => c.RefreshToken.Token == "old"), It.IsAny<CancellationToken>()), Times.Once);
            NoCookiesShouldBeSet(sut);
        }

        [TestMethod]
        public async Task RefreshAsync_RejectedRefreshToken_ReturnsBadRequest()
        {
            _sender.Setup(s => s.Send(It.IsAny<RefreshCommand>(), It.IsAny<CancellationToken>())).ReturnsAsync(Result<TokenResult>.Failure("Refresh token not found"));
            var sut = CreateSut();

            var result = await sut.RefreshAsync(new RefreshRequestModel { RefreshToken = new RefreshToken { Token = "old", ExpiresAfter = AccessExpiry } });

            result.Should().BeOfType<BadRequestObjectResult>();
        }

        [TestMethod]
        public async Task LogoutAsync_RefreshToken_RevokesItAndReturnsNoContentWithoutCookies()
        {
            var sut = CreateSut();

            var result = await sut.LogoutAsync(new RefreshRequestModel { RefreshToken = new RefreshToken { Token = "old", ExpiresAfter = AccessExpiry } });

            result.Should().BeOfType<NoContentResult>();
            _sender.Verify(s => s.Send(It.Is<LogoutCommand>(c => c.RefreshToken!.Token == "old"), It.IsAny<CancellationToken>()), Times.Once);
            NoCookiesShouldBeSet(sut);
        }
    }
}