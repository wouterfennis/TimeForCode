using AwesomeAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using TimeForCode.Authorization.Application.Handlers.Admin;
using TimeForCode.Authorization.Application.Interfaces.Admin;
using TimeForCode.Authorization.Application.Services;
using TimeForCode.Authorization.Commands.Admin;
using TimeForCode.Authorization.Domain.Entities;
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
            _tokenServiceMock.Setup(x => x.GenerateInternalToken("admin", "admin", "admin"))
                .Returns(new AccessToken { Token = "jwt", ExpiresAfter = DateTimeOffset.UtcNow.AddMinutes(15) });
            _refreshTokenServiceMock.Setup(x => x.CreateRefreshTokenAsync("admin"))
                .ReturnsAsync(new Values.RefreshToken { Token = "refresh", ExpiresAfter = DateTimeOffset.UtcNow.AddDays(7) });
            StoredCounter(3);
            _repositoryMock.Setup(x => x.TryAdvanceSignCountAsync(It.IsAny<long>())).ReturnsAsync(true);

            _sut = new CompleteAdminAuthenticationHandler(
                _repositoryMock.Object,
                _ceremonyServiceMock.Object,
                _tokenServiceMock.Object,
                _refreshTokenServiceMock.Object,
                NullLogger<CompleteAdminAuthenticationHandler>.Instance);
        }

        private void StoredCounter(long signCount) =>
            _repositoryMock.Setup(x => x.GetAsync()).ReturnsAsync(new AdminCredential
            {
                Id = AdminCredential.SingletonId,
                CredentialId = [1],
                PublicKey = [2],
                SignCount = signCount,
                Transports = [],
                IsUserVerified = true,
                IsBackupEligible = false,
                IsBackedUp = false,
                CreatedAt = DateTimeOffset.UtcNow
            });

        private void AssertionSucceedsWithCounter(long signCount) =>
            _ceremonyServiceMock.Setup(x => x.CompleteAuthenticationAsync(It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync(new AdminAssertionOutcome { Succeeded = true, SignCount = signCount });

        private static CompleteAdminAuthenticationCommand ValidCommand() => new()
        {
            CredentialJson = "credential-json",
            CeremonyId = "ceremony"
        };

        [TestMethod]
        public async Task Handle_SuccessfulAssertion_ReturnsSuccessAndUpdatesSignCount()
        {
            AssertionSucceedsWithCounter(5);

            var result = await _sut.Handle(ValidCommand(), CancellationToken.None);

            result.IsSuccess.Should().BeTrue();
            result.Value.InternalAccessToken.Token.Should().Be("jwt");
            _repositoryMock.Verify(x => x.TryAdvanceSignCountAsync(5), Times.Once);
        }

        [TestMethod]
        public async Task Handle_AssertionFails_ReturnsFailure()
        {
            _ceremonyServiceMock.Setup(x => x.CompleteAuthenticationAsync(It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync(new AdminAssertionOutcome { Succeeded = false, ErrorMessage = "bad device" });

            var result = await _sut.Handle(ValidCommand(), CancellationToken.None);

            result.IsFailure.Should().BeTrue();
            _repositoryMock.Verify(x => x.TryAdvanceSignCountAsync(It.IsAny<long>()), Times.Never);
            _tokenServiceMock.Verify(x => x.GenerateInternalToken(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()), Times.Never);
        }

        [TestMethod]
        public async Task Handle_PassesTheCeremonyIdToTheCeremonyService()
        {
            AssertionSucceedsWithCounter(5);

            await _sut.Handle(ValidCommand(), CancellationToken.None);

            _ceremonyServiceMock.Verify(x => x.CompleteAuthenticationAsync("credential-json", "ceremony"), Times.Once);
        }

        [TestMethod]
        [DataRow(3L, 3L)]
        [DataRow(3L, 2L)]
        [DataRow(3L, 0L)]
        public async Task Handle_CounterDidNotIncrease_RejectsAClonedAuthenticatorOrReplayedAssertion(long stored, long presented)
        {
            StoredCounter(stored);
            AssertionSucceedsWithCounter(presented);

            var result = await _sut.Handle(ValidCommand(), CancellationToken.None);

            result.IsFailure.Should().BeTrue();
            _tokenServiceMock.Verify(x => x.GenerateInternalToken(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()), Times.Never);
            _refreshTokenServiceMock.Verify(x => x.CreateRefreshTokenAsync(It.IsAny<string>()), Times.Never);
        }

        [TestMethod]
        [DataRow(0L, 0L)]
        [DataRow(0L, 1L)]
        [DataRow(3L, 4L)]
        public async Task Handle_CounterIncreasedOrTheAuthenticatorHasNone_Succeeds(long stored, long presented)
        {
            StoredCounter(stored);
            AssertionSucceedsWithCounter(presented);

            var result = await _sut.Handle(ValidCommand(), CancellationToken.None);

            result.IsSuccess.Should().BeTrue("synced passkeys always report 0, and a first use may move 0 to 1");
        }

        [TestMethod]
        public async Task Handle_CounterLooksFineButAnotherAssertionAdvancedItFirst_RejectsTheLoserOfTheRace()
        {
            AssertionSucceedsWithCounter(5);
            _repositoryMock.Setup(x => x.TryAdvanceSignCountAsync(5)).ReturnsAsync(false);

            var result = await _sut.Handle(ValidCommand(), CancellationToken.None);

            result.IsFailure.Should().BeTrue("two concurrent assertions must not both pass against the same stored counter");
            _tokenServiceMock.Verify(x => x.GenerateInternalToken(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()), Times.Never);
        }

        [TestMethod]
        public async Task Handle_NoAdminCredentialRegistered_ReturnsFailureWithoutCheckingTheAssertion()
        {
            _repositoryMock.Setup(x => x.GetAsync()).ReturnsAsync((AdminCredential?)null);

            var result = await _sut.Handle(ValidCommand(), CancellationToken.None);

            result.IsFailure.Should().BeTrue();
            _ceremonyServiceMock.Verify(x => x.CompleteAuthenticationAsync(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
        }
    }
}