using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;
using TimeForCode.Authorization.Application.Handlers.Admin;
using TimeForCode.Authorization.Application.Interfaces;
using TimeForCode.Authorization.Application.Interfaces.Admin;
using TimeForCode.Authorization.Application.Options;
using TimeForCode.Authorization.Application.Services;
using TimeForCode.Authorization.Commands.Admin;
using TimeForCode.Authorization.Domain.Entities;
using TimeForCode.Shared.Api.Authentication.Models;

namespace TimeForCode.Authorization.Application.Tests.Handlers.Admin
{
    [TestClass]
    public class CompleteAdminRegistrationHandlerTests
    {
        private Mock<IAdminCredentialRepository> _repositoryMock = null!;
        private Mock<IPasskeyCeremonyService> _ceremonyServiceMock = null!;
        private Mock<ITokenService> _tokenServiceMock = null!;
        private Mock<IRefreshTokenService> _refreshTokenServiceMock = null!;
        private CompleteAdminRegistrationHandler _sut = null!;

        [TestInitialize]
        public void Setup()
        {
            _repositoryMock = new Mock<IAdminCredentialRepository>();
            _ceremonyServiceMock = new Mock<IPasskeyCeremonyService>();
            _tokenServiceMock = new Mock<ITokenService>();
            _refreshTokenServiceMock = new Mock<IRefreshTokenService>();

            var options = Microsoft.Extensions.Options.Options.Create(new AdminPasskeyOptions
            {
                ServerDomain = "localhost",
                RelyingPartyName = "TimeForCode Admin",
                BootstrapSecret = "correct-secret"
            });

            _sut = new CompleteAdminRegistrationHandler(
                _repositoryMock.Object,
                _ceremonyServiceMock.Object,
                _tokenServiceMock.Object,
                _refreshTokenServiceMock.Object,
                TimeProvider.System,
                options,
                NullLogger<CompleteAdminRegistrationHandler>.Instance);
        }

        private static CompleteAdminRegistrationCommand ValidCommand() => new()
        {
            BootstrapSecret = "correct-secret",
            CredentialJson = "credential-json",
            ProtectedCeremonyState = "state"
        };

        [TestMethod]
        public async Task Handle_SuccessfulAttestationAndFirstClaim_ReturnsSuccessWithTokens()
        {
            _ceremonyServiceMock.Setup(x => x.CompleteRegistrationAsync(It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync(new AdminAttestationOutcome
                {
                    Succeeded = true,
                    CredentialId = [1],
                    PublicKey = [2],
                    SignCount = 0,
                    Transports = ["internal"],
                    IsUserVerified = true,
                    IsBackupEligible = false,
                    IsBackedUp = false
                });
            _repositoryMock.Setup(x => x.TryClaimAsync(It.IsAny<AdminCredential>())).ReturnsAsync(true);
            _tokenServiceMock.Setup(x => x.GenerateInternalToken("admin", "admin", "admin"))
                .Returns(new AccessToken { Token = "jwt", ExpiresAfter = DateTimeOffset.UtcNow.AddMinutes(60) });
            _refreshTokenServiceMock.Setup(x => x.CreateRefreshTokenAsync("admin"))
                .ReturnsAsync(new Values.RefreshToken { Token = "refresh", ExpiresAfter = DateTimeOffset.UtcNow.AddDays(7) });

            var result = await _sut.Handle(ValidCommand(), CancellationToken.None);

            result.IsSuccess.Should().BeTrue();
            result.Value.InternalAccessToken.Token.Should().Be("jwt");
        }

        [TestMethod]
        public async Task Handle_IncorrectBootstrapSecret_ReturnsFailure()
        {
            var invalidCommand = new CompleteAdminRegistrationCommand
            {
                BootstrapSecret = "wrong-secret",
                CredentialJson = "credential-json",
                ProtectedCeremonyState = "state"
            };

            var result = await _sut.Handle(invalidCommand, CancellationToken.None);

            result.IsFailure.Should().BeTrue();
            _ceremonyServiceMock.Verify(x => x.CompleteRegistrationAsync(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
        }

        [TestMethod]
        public async Task Handle_AttestationFails_ReturnsFailure()
        {
            _ceremonyServiceMock.Setup(x => x.CompleteRegistrationAsync(It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync(new AdminAttestationOutcome { Succeeded = false, ErrorMessage = "bad ceremony" });

            var result = await _sut.Handle(ValidCommand(), CancellationToken.None);

            result.IsFailure.Should().BeTrue();
            _repositoryMock.Verify(x => x.TryClaimAsync(It.IsAny<AdminCredential>()), Times.Never);
        }

        [TestMethod]
        public async Task Handle_ClaimRaceLost_ReturnsFailure()
        {
            _ceremonyServiceMock.Setup(x => x.CompleteRegistrationAsync(It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync(new AdminAttestationOutcome
                {
                    Succeeded = true,
                    CredentialId = [1],
                    PublicKey = [2],
                    SignCount = 0,
                    Transports = [],
                    IsUserVerified = true,
                    IsBackupEligible = false,
                    IsBackedUp = false
                });
            _repositoryMock.Setup(x => x.TryClaimAsync(It.IsAny<AdminCredential>())).ReturnsAsync(false);

            var result = await _sut.Handle(ValidCommand(), CancellationToken.None);

            result.IsFailure.Should().BeTrue();
            _tokenServiceMock.Verify(x => x.GenerateInternalToken(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()), Times.Never);
        }
    }
}