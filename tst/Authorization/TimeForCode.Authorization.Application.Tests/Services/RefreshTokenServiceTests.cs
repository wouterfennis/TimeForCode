using AwesomeAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using MongoDB.Bson;
using Moq;
using TimeForCode.Authorization.Application.Interfaces;
using TimeForCode.Authorization.Application.Options;
using TimeForCode.Authorization.Application.Services;

namespace TimeForCode.Authorization.Application.Tests.Services
{
    [TestClass]
    public class RefreshTokenServiceTests
    {
        private static readonly DateTimeOffset Now = new(2026, 10, 16, 12, 0, 0, TimeSpan.Zero);

        private Mock<IRefreshTokenRepository> _repositoryMock = null!;
        private Mock<IRandomGenerator> _randomGeneratorMock = null!;
        private RefreshTokenService _sut = null!;

        [TestInitialize]
        public void Setup()
        {
            _repositoryMock = new Mock<IRefreshTokenRepository>();
            _randomGeneratorMock = new Mock<IRandomGenerator>();
            _randomGeneratorMock.Setup(x => x.GenerateRandomString()).Returns("plain-token");

            var timeProvider = new Mock<TimeProvider>();
            timeProvider.Setup(x => x.GetUtcNow()).Returns(Now);

            _sut = new RefreshTokenService(
                timeProvider.Object,
                _randomGeneratorMock.Object,
                _repositoryMock.Object,
                Microsoft.Extensions.Options.Options.Create(new TokenCreationOptions { Issuer = "issuer", Audiences = ["audience"], TokenExpiresInMinutes = 60, DefaultRefreshTokenExpirationAfterInDays = 7 }),
                NullLogger<RefreshTokenService>.Instance);
        }

        [TestMethod]
        public async Task CreateRefreshTokenAsync_StoresHashInsteadOfToken()
        {
            Domain.Entities.RefreshToken? stored = null;
            _repositoryMock.Setup(x => x.CreateAsync(It.IsAny<Domain.Entities.RefreshToken>()))
                .Callback<Domain.Entities.RefreshToken>(t => stored = t)
                .Returns(Task.CompletedTask);

            var result = await _sut.CreateRefreshTokenAsync("user-1");

            result.Token.Should().Be("plain-token");
            stored.Should().NotBeNull();
            stored!.TokenHash.Should().NotBe("plain-token");
            stored.TokenHash.Should().Be(RefreshTokenHasher.Hash("plain-token"));
        }

        [TestMethod]
        public async Task ReplaceRefreshTokenAsync_UnknownToken_Fails()
        {
            _repositoryMock.Setup(x => x.GetByTokenHashAsync(It.IsAny<string>()))
                .ReturnsAsync((Domain.Entities.RefreshToken?)null);

            var result = await _sut.ReplaceRefreshTokenAsync(Token("unknown"));

            result.IsFailure.Should().BeTrue();
            _repositoryMock.Verify(x => x.DeleteByUserIdAsync(It.IsAny<string>()), Times.Never);
        }

        [TestMethod]
        public async Task ReplaceRefreshTokenAsync_ExpiredToken_FailsWithoutRevokingOrRotating()
        {
            Domain.Entities.RefreshToken existing = Entity("plain-old", rotatedAt: null, expiresAfter: Now.AddMinutes(-1));
            _repositoryMock.Setup(x => x.GetByTokenHashAsync(RefreshTokenHasher.Hash("plain-old")))
                .ReturnsAsync(existing);

            var result = await _sut.ReplaceRefreshTokenAsync(Token("plain-old"));

            result.IsFailure.Should().BeTrue();
            _repositoryMock.Verify(x => x.TryMarkRotatedAsync(It.IsAny<ObjectId>(), It.IsAny<DateTimeOffset>()), Times.Never);
            _repositoryMock.Verify(x => x.DeleteByUserIdAsync(It.IsAny<string>()), Times.Never);
        }

        [TestMethod]
        public async Task ReplaceRefreshTokenAsync_ActiveToken_RotatesAndReturnsNewTokenWithUserId()
        {
            Domain.Entities.RefreshToken existing = Entity("plain-old", rotatedAt: null);
            _repositoryMock.Setup(x => x.GetByTokenHashAsync(RefreshTokenHasher.Hash("plain-old")))
                .ReturnsAsync(existing);
            _repositoryMock.Setup(x => x.TryMarkRotatedAsync(existing.Id, Now)).ReturnsAsync(true);

            var result = await _sut.ReplaceRefreshTokenAsync(Token("plain-old"));

            result.IsSuccess.Should().BeTrue();
            result.Value.UserId.Should().Be("user-1");
            result.Value.RefreshToken.Token.Should().Be("plain-token");
            _repositoryMock.Verify(x => x.CreateAsync(It.Is<Domain.Entities.RefreshToken>(t => t.UserId == "user-1")), Times.Once);
            _repositoryMock.Verify(x => x.DeleteByUserIdAsync(It.IsAny<string>()), Times.Never);
        }

        [TestMethod]
        public async Task ReplaceRefreshTokenAsync_AlreadyRotatedToken_FailsAndRevokesAllTokensOfUser()
        {
            Domain.Entities.RefreshToken existing = Entity("plain-old", rotatedAt: Now.AddMinutes(-1));
            _repositoryMock.Setup(x => x.GetByTokenHashAsync(RefreshTokenHasher.Hash("plain-old")))
                .ReturnsAsync(existing);

            var result = await _sut.ReplaceRefreshTokenAsync(Token("plain-old"));

            result.IsFailure.Should().BeTrue();
            _repositoryMock.Verify(x => x.DeleteByUserIdAsync("user-1"), Times.Once);
            _repositoryMock.Verify(x => x.CreateAsync(It.IsAny<Domain.Entities.RefreshToken>()), Times.Never);
        }

        [TestMethod]
        public async Task ReplaceRefreshTokenAsync_LostRotationRace_FailsAndRevokesAllTokensOfUser()
        {
            Domain.Entities.RefreshToken existing = Entity("plain-old", rotatedAt: null);
            _repositoryMock.Setup(x => x.GetByTokenHashAsync(RefreshTokenHasher.Hash("plain-old")))
                .ReturnsAsync(existing);
            _repositoryMock.Setup(x => x.TryMarkRotatedAsync(existing.Id, Now)).ReturnsAsync(false);

            var result = await _sut.ReplaceRefreshTokenAsync(Token("plain-old"));

            result.IsFailure.Should().BeTrue();
            _repositoryMock.Verify(x => x.DeleteByUserIdAsync("user-1"), Times.Once);
            _repositoryMock.Verify(x => x.CreateAsync(It.IsAny<Domain.Entities.RefreshToken>()), Times.Never);
        }

        [TestMethod]
        public async Task ExpireRefreshTokenAsync_KnownToken_DeletesByHash()
        {
            Domain.Entities.RefreshToken existing = Entity("plain-old", rotatedAt: null);
            _repositoryMock.Setup(x => x.GetByTokenHashAsync(RefreshTokenHasher.Hash("plain-old")))
                .ReturnsAsync(existing);

            await _sut.ExpireRefreshTokenAsync(Token("plain-old"));

            _repositoryMock.Verify(x => x.DeleteAsync(existing), Times.Once);
        }

        private static Values.RefreshToken Token(string token) => new()
        {
            Token = token,
            ExpiresAfter = Now.AddDays(1)
        };

        private static Domain.Entities.RefreshToken Entity(string token, DateTimeOffset? rotatedAt, DateTimeOffset? expiresAfter = null) => WithRotation(new Domain.Entities.RefreshToken
        {
            Id = ObjectId.GenerateNewId(),
            TokenHash = RefreshTokenHasher.Hash(token),
            UserId = "user-1",
            ExpiresAfter = expiresAfter ?? Now.AddDays(1)
        }, rotatedAt);

        private static Domain.Entities.RefreshToken WithRotation(Domain.Entities.RefreshToken token, DateTimeOffset? rotatedAt)
        {
            if (rotatedAt.HasValue)
            {
                token.MarkRotated(rotatedAt.Value);
            }

            return token;
        }
    }
}