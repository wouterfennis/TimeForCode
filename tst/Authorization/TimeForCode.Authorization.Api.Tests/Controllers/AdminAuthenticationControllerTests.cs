using AwesomeAssertions;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using System.Text.Json;
using TimeForCode.Authorization.Api.Controllers;
using TimeForCode.Authorization.Api.Models;
using TimeForCode.Authorization.Api.Models.Admin;
using TimeForCode.Authorization.Commands;
using TimeForCode.Authorization.Commands.Admin;
using TimeForCode.Authorization.Values;
using TimeForCode.Shared.Api.Authentication.Models;

namespace TimeForCode.Authorization.Api.Tests.Controllers
{
    [TestClass]
    public class AdminAuthenticationControllerTests
    {
        private static readonly DateTimeOffset AccessExpiry = new(2030, 1, 1, 0, 0, 0, TimeSpan.Zero);

        private readonly Mock<ISender> _sender = new();
        private DefaultHttpContext _context = null!;

        private AdminAuthenticationController CreateSut()
        {
            _context = new DefaultHttpContext();
            return new AdminAuthenticationController(_sender.Object, NullLogger<AdminAuthenticationController>.Instance)
            {
                ControllerContext = new ControllerContext { HttpContext = _context }
            };
        }

        private static Result<TokenResult> Tokens() => Result<TokenResult>.Success(new TokenResult
        {
            InternalAccessToken = new AccessToken { Token = "access-secret", ExpiresAfter = AccessExpiry },
            RefreshToken = new RefreshToken { Token = "refresh-secret", ExpiresAfter = AccessExpiry.AddDays(7) }
        });

        private void NoCookiesShouldBeSet() => _context.Response.Headers.SetCookie.Should().BeEmpty();

        [TestMethod]
        public async Task RegistrationOptionsAsync_Success_ReturnsTheOptionsAndTheCeremonyIdWithoutACookie()
        {
            _sender.Setup(s => s.Send(It.IsAny<CreateAdminRegistrationOptionsCommand>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(Result<PasskeyCeremonyOptionsResult>.Success(new PasskeyCeremonyOptionsResult { OptionsJson = "{\"challenge\":\"c\"}", CeremonyId = "ceremony-1" }));
            var sut = CreateSut();

            var result = await sut.RegistrationOptionsAsync(new AdminRegistrationOptionsRequestModel { BootstrapSecret = "secret" });

            var body = result.Should().BeOfType<OkObjectResult>().Subject.Value.Should().BeOfType<AdminPasskeyOptionsResponseModel>().Subject;
            body.CeremonyId.Should().Be("ceremony-1");
            body.OptionsJson.Should().Be("{\"challenge\":\"c\"}");
            NoCookiesShouldBeSet();
        }

        [TestMethod]
        public async Task RegistrationOptionsAsync_WrongSecret_ReturnsBadRequest()
        {
            _sender.Setup(s => s.Send(It.IsAny<CreateAdminRegistrationOptionsCommand>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(Result<PasskeyCeremonyOptionsResult>.Failure("Invalid bootstrap secret."));
            var sut = CreateSut();

            var result = await sut.RegistrationOptionsAsync(new AdminRegistrationOptionsRequestModel { BootstrapSecret = "wrong" });

            var problem = result.Should().BeOfType<BadRequestObjectResult>().Subject.Value.Should().BeOfType<ProblemDetails>().Subject;
            problem.Detail.Should().NotContain("secret", "the reason of a refusal is not given away");
        }

        [TestMethod]
        public async Task CompleteRegistrationAsync_Success_ReturnsTokensInTheBodyAndSetsNoCookie()
        {
            _sender.Setup(s => s.Send(It.IsAny<CompleteAdminRegistrationCommand>(), It.IsAny<CancellationToken>())).ReturnsAsync(Tokens());
            var sut = CreateSut();

            var result = await sut.CompleteRegistrationAsync(new AdminCompleteRegistrationRequestModel { BootstrapSecret = "secret", CredentialJson = "{}", CeremonyId = "ceremony-1" });

            var body = result.Should().BeOfType<OkObjectResult>().Subject.Value.Should().BeOfType<TokenResponseModel>().Subject;
            body.AccessToken.Token.Should().Be("access-secret");
            body.RefreshToken.Token.Should().Be("refresh-secret");
            NoCookiesShouldBeSet();
            _sender.Verify(s => s.Send(It.Is<CompleteAdminRegistrationCommand>(c => c.CeremonyId == "ceremony-1"), It.IsAny<CancellationToken>()), Times.Once);
        }

        [TestMethod]
        public async Task CompleteRegistrationAsync_Failure_ReturnsBadRequest()
        {
            _sender.Setup(s => s.Send(It.IsAny<CompleteAdminRegistrationCommand>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(Result<TokenResult>.Failure("Admin passkey registration failed."));
            var sut = CreateSut();

            var result = await sut.CompleteRegistrationAsync(new AdminCompleteRegistrationRequestModel { BootstrapSecret = "secret", CredentialJson = "{}", CeremonyId = "ceremony-1" });

            result.Should().BeOfType<BadRequestObjectResult>();
        }

        [TestMethod]
        public async Task AuthenticationOptionsAsync_Success_ReturnsTheOptionsAndTheCeremonyIdWithoutACookie()
        {
            _sender.Setup(s => s.Send(It.IsAny<CreateAdminAuthenticationOptionsCommand>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(Result<PasskeyCeremonyOptionsResult>.Success(new PasskeyCeremonyOptionsResult { OptionsJson = "{}", CeremonyId = "ceremony-2" }));
            var sut = CreateSut();

            var result = await sut.AuthenticationOptionsAsync();

            var body = result.Should().BeOfType<OkObjectResult>().Subject.Value.Should().BeOfType<AdminPasskeyOptionsResponseModel>().Subject;
            body.CeremonyId.Should().Be("ceremony-2");
            NoCookiesShouldBeSet();
        }

        [TestMethod]
        public async Task CompleteAuthenticationAsync_Success_ReturnsTokensInTheBodyAndSetsNoCookie()
        {
            _sender.Setup(s => s.Send(It.IsAny<CompleteAdminAuthenticationCommand>(), It.IsAny<CancellationToken>())).ReturnsAsync(Tokens());
            var sut = CreateSut();

            var result = await sut.CompleteAuthenticationAsync(new AdminCompleteAuthenticationRequestModel { CredentialJson = "{}", CeremonyId = "ceremony-2" });

            var body = result.Should().BeOfType<OkObjectResult>().Subject.Value.Should().BeOfType<TokenResponseModel>().Subject;
            JsonSerializer.Serialize(body).Should().Contain("access-secret");
            NoCookiesShouldBeSet();
            _sender.Verify(s => s.Send(It.Is<CompleteAdminAuthenticationCommand>(c => c.CeremonyId == "ceremony-2" && c.CredentialJson == "{}"), It.IsAny<CancellationToken>()), Times.Once);
        }

        [TestMethod]
        public async Task CompleteAuthenticationAsync_Failure_ReturnsBadRequest()
        {
            _sender.Setup(s => s.Send(It.IsAny<CompleteAdminAuthenticationCommand>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(Result<TokenResult>.Failure("Admin authentication failed."));
            var sut = CreateSut();

            var result = await sut.CompleteAuthenticationAsync(new AdminCompleteAuthenticationRequestModel { CredentialJson = "{}", CeremonyId = "ceremony-2" });

            result.Should().BeOfType<BadRequestObjectResult>();
        }
    }
}