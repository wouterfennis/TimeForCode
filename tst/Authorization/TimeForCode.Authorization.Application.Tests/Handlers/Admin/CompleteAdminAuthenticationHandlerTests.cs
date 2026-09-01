using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using TimeForCode.Authorization.Application.Handlers.Admin;
using TimeForCode.Authorization.Application.Interfaces.Admin;
using TimeForCode.Authorization.Application.Services;
using TimeForCode.Authorization.Commands.Admin;
using TimeForCode.Shared.Api.Authentication.Models;

namespace TimeForCode.Authorization.Application.Tests.Handlers.Admin
{
    [TestClass]
    public class CompleteAdminAuthenticationHandlerTests
    {
        private Mock<IAdminCredentialRepository> _repositoryMock = null!;
        private Mock<IPasskeyCeremonyService> _ceremonyServiceMock = null!;
        private Mock<ITokenService> _tokenServiceMock = null!;
        private Mock<IRefreshTokenService> _refreshTokenServiceMock = null!;
        private CompleteAdminAuthenticationHandler _sut = null!;

        [TestInitialize]
        public void Setup()
        {
            _repositoryMock = new Mock<IAdminCredentialRepository>();
            _ceremonyServiceMock = new Mock<IPasskeyCeremonyService>();
            _tokenServiceMock = new Mock<ITokenService>();
            _refreshTokenServiceMock = new Mock<IRefreshTokenService>();

            _sut = new CompleteAdminAuthenticationHandler(
                _repositoryMock.Object,
                _ceremonyServiceMock.Object,
                _tokenServiceMock.Object,
                _refreshTokenServiceMock.Object,
                NullLogger<CompleteAdminAuthenticationHandler>.Instance);
        }

        private static CompleteAdminAuthenticationCommand ValidCommand() => new()
        {
            CredentialJson = "credential-json",
            ProtectedCeremonyState = "state"
        };

        [TestMethod]
        public async Task Handle_SuccessfulAssertion_ReturnsSuccessAndUpdatesSignCount()
        {
            _ceremonyServiceMock.Setup(x => x.CompleteAuthenticationAsync(It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync(new AdminAssertionOutcome { Succeeded = true, SignCount = 5 });
            _tokenServiceMock.Setup(x => x.GenerateInternalToken("admin", "admin", "admin"))
                .Returns(new AccessToken { Token = "jwt", ExpiresAfter = DateTimeOffset.UtcNow.AddMinutes(60) });
            _refreshTokenServiceMock.Setup(x => x.CreateRefreshTokenAsync("admin"))
                .ReturnsAsync(new Values.RefreshToken { Token = "refresh", ExpiresAfter = DateTimeOffset.UtcNow.AddDays(7) });

            var result = await _sut.Handle(ValidCommand(), CancellationToken.None);

            result.IsSuccess.Should().BeTrue();
            result.Value.InternalAccessToken.Token.Should().Be("jwt");
            _repositoryMock.Verify(x => x.UpdateSignCountAsync(5), Times.Once);
        }

        [TestMethod]
        public async Task Handle_AssertionFails_ReturnsFailure()
        {
            _ceremonyServiceMock.Setup(x => x.CompleteAuthenticationAsync(It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync(new AdminAssertionOutcome { Succeeded = false, ErrorMessage = "bad device" });

            var result = await _sut.Handle(ValidCommand(), CancellationToken.None);

            result.IsFailure.Should().BeTrue();
            _repositoryMock.Verify(x => x.UpdateSignCountAsync(It.IsAny<long>()), Times.Never);
            _tokenServiceMock.Verify(x => x.GenerateInternalToken(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()), Times.Never);
        }
    }
}