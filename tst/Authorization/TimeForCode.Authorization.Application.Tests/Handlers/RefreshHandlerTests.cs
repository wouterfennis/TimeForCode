using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using TimeForCode.Authorization.Application.Handlers;
using TimeForCode.Authorization.Application.Services;
using TimeForCode.Authorization.Commands;
using TimeForCode.Shared.Api.Authentication.Models;

namespace TimeForCode.Authorization.Application.Tests.Handlers
{
    [TestClass]
    public class RefreshHandlerTests
    {
        private static readonly DateTimeOffset Now = new(2026, 10, 16, 12, 0, 0, TimeSpan.Zero);

        private Mock<ITokenService> _tokenServiceMock = null!;
        private Mock<IRefreshTokenService> _refreshTokenServiceMock = null!;
        private RefreshHandler _sut = null!;

        [TestInitialize]
        public void Setup()
        {
            _tokenServiceMock = new Mock<ITokenService>();
            _refreshTokenServiceMock = new Mock<IRefreshTokenService>();
            _sut = new RefreshHandler(_tokenServiceMock.Object, _refreshTokenServiceMock.Object, NullLogger<RefreshHandler>.Instance);
        }

        private static RefreshCommand Command() => new()
        {
            RefreshToken = new Values.RefreshToken { Token = "old", ExpiresAfter = Now.AddDays(1) }
        };

        [TestMethod]
        public async Task Handle_ReplacementSucceeds_IssuesAccessTokenForTheReplacedSessionUser()
        {
            var newRefreshToken = new Values.RefreshToken { Token = "new", ExpiresAfter = Now.AddDays(7) };
            _refreshTokenServiceMock.Setup(x => x.ReplaceRefreshTokenAsync(It.IsAny<Values.RefreshToken>()))
                .ReturnsAsync(Result<RefreshTokenReplacement>.Success(new RefreshTokenReplacement(newRefreshToken, "user-1")));
            _tokenServiceMock.Setup(x => x.GenerateInternalTokenForSession("user-1"))
                .Returns(new AccessToken { Token = "jwt", ExpiresAfter = Now.AddMinutes(60) });

            var result = await _sut.Handle(Command(), CancellationToken.None);

            result.IsSuccess.Should().BeTrue();
            result.Value.InternalAccessToken.Token.Should().Be("jwt");
            result.Value.RefreshToken.Token.Should().Be("new");
        }

        [TestMethod]
        public async Task Handle_ReplacementFails_FailsWithoutIssuingAccessToken()
        {
            _refreshTokenServiceMock.Setup(x => x.ReplaceRefreshTokenAsync(It.IsAny<Values.RefreshToken>()))
                .ReturnsAsync(Result<RefreshTokenReplacement>.Failure("Refresh token reuse detected"));

            var result = await _sut.Handle(Command(), CancellationToken.None);

            result.IsFailure.Should().BeTrue();
            _tokenServiceMock.Verify(x => x.GenerateInternalTokenForSession(It.IsAny<string>()), Times.Never);
        }
    }
}