using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;
using TimeForCode.Authorization.Application.Handlers.Admin;
using TimeForCode.Authorization.Application.Options;
using TimeForCode.Authorization.Application.Services;
using TimeForCode.Authorization.Commands.Admin;
using TimeForCode.Shared.Api.Authentication.Models;

namespace TimeForCode.Authorization.Application.Tests.Handlers.Admin
{
    [TestClass]
    public class CreateTestAdminTokenHandlerTests
    {
        private const string Secret = "a-long-enough-test-secret";

        private Mock<ITokenService> _tokenServiceMock = null!;
        private Mock<IRefreshTokenService> _refreshTokenServiceMock = null!;

        [TestInitialize]
        public void Setup()
        {
            _tokenServiceMock = new Mock<ITokenService>();
            _refreshTokenServiceMock = new Mock<IRefreshTokenService>();
            _tokenServiceMock.Setup(x => x.GenerateInternalToken("admin", "admin", "admin"))
                .Returns(new AccessToken { Token = "jwt", ExpiresAfter = DateTimeOffset.UtcNow.AddMinutes(60) });
            _refreshTokenServiceMock.Setup(x => x.CreateRefreshTokenAsync("admin"))
                .ReturnsAsync(new Values.RefreshToken { Token = "refresh", ExpiresAfter = DateTimeOffset.UtcNow.AddDays(7) });
        }

        private CreateTestAdminTokenHandler CreateSut(bool enabled) => new(
            _tokenServiceMock.Object,
            _refreshTokenServiceMock.Object,
            Microsoft.Extensions.Options.Options.Create(new TestAdminAccessOptions { Enabled = enabled, Secret = Secret }),
            NullLogger<CreateTestAdminTokenHandler>.Instance);

        [TestMethod]
        public async Task Handle_EnabledAndCorrectSecret_ReturnsAdminToken()
        {
            var result = await CreateSut(enabled: true).Handle(new CreateTestAdminTokenCommand { Secret = Secret }, CancellationToken.None);

            result.IsSuccess.Should().BeTrue();
            result.Value.InternalAccessToken.Token.Should().Be("jwt");
            result.Value.RefreshToken.Token.Should().Be("refresh");
        }

        [TestMethod]
        public async Task Handle_EnabledAndWrongSecret_ReturnsFailureWithoutToken()
        {
            var result = await CreateSut(enabled: true).Handle(new CreateTestAdminTokenCommand { Secret = "wrong-secret-value-here" }, CancellationToken.None);

            result.IsFailure.Should().BeTrue();
            _tokenServiceMock.Verify(x => x.GenerateInternalToken(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()), Times.Never);
        }

        [TestMethod]
        public async Task Handle_Disabled_ReturnsFailureEvenWithCorrectSecret()
        {
            var result = await CreateSut(enabled: false).Handle(new CreateTestAdminTokenCommand { Secret = Secret }, CancellationToken.None);

            result.IsFailure.Should().BeTrue();
            _tokenServiceMock.Verify(x => x.GenerateInternalToken(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()), Times.Never);
        }
    }
}