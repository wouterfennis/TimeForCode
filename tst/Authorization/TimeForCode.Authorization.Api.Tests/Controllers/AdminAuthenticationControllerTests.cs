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
using TimeForCode.Authorization.Api.Models.Admin;
using TimeForCode.Authorization.Application.Options;
using TimeForCode.Authorization.Commands;
using TimeForCode.Authorization.Commands.Admin;
using TimeForCode.Authorization.Values;
using TimeForCode.Shared.Api.Authentication;
using TimeForCode.Shared.Api.Authentication.Models;

namespace TimeForCode.Authorization.Api.Tests.Controllers
{
    [TestClass]
    public class AdminAuthenticationControllerTests
    {
        private const string RefreshPath = "/api/auth/v1/Authentication/session";

        private static readonly DateTimeOffset AccessExpiry = new(2030, 1, 1, 0, 0, 0, TimeSpan.Zero);

        [TestMethod]
        public async Task CompleteAuthenticationAsync_ValidAssertion_ReturnsNoTokensInTheBodyAndScopesTheRefreshCookie()
        {
            var sender = new Mock<ISender>();
            sender.Setup(s => s.Send(It.IsAny<CompleteAdminAuthenticationCommand>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(Result<TokenResult>.Success(new TokenResult
                {
                    InternalAccessToken = new AccessToken { Token = "access-secret", ExpiresAfter = AccessExpiry },
                    RefreshToken = new RefreshToken { Token = "refresh-secret", ExpiresAfter = AccessExpiry.AddDays(7) }
                }));
            var context = new DefaultHttpContext();
            context.Request.Headers.Cookie = $"{AdminAuthenticationController.AssertionStateCookieKey}=state";
            var sut = new AdminAuthenticationController(sender.Object,
                Options.Create(new AuthenticationOptions { Issuer = "i", Audience = "a", ValidRedirectUris = [], RefreshCookiePath = RefreshPath }),
                NullLogger<AdminAuthenticationController>.Instance)
            {
                ControllerContext = new ControllerContext { HttpContext = context }
            };

            var result = await sut.CompleteAuthenticationAsync(new AdminCompleteAuthenticationRequestModel { CredentialJson = "{}" });

            var body = result.Should().BeOfType<OkObjectResult>().Subject.Value.Should().BeOfType<CallbackResponseModel>().Subject;
            JsonSerializer.Serialize(body).Should().NotContain("access-secret").And.NotContain("refresh-secret");
            context.Response.Headers.SetCookie.ToArray().Single(c => c!.StartsWith(CookieConstants.RefreshTokenKey + "="))
                .Should().Contain($"path={RefreshPath}");
        }
    }
}