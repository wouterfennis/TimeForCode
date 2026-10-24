using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using System.Net;
using TimeForCode.Donation.Application.Handlers;
using TimeForCode.Donation.Application.Interfaces;
using TimeForCode.Donation.Commands;
using TimeForCode.Donation.Domain;

namespace TimeForCode.Donation.Application.Tests.Handlers
{
    [TestClass]
    public class GetMaintainerProfileHandlerTests
    {
        private Mock<IMaintainerRepository> _mockRepository = default!;
        private GetMaintainerProfileHandler _sut = default!;

        [TestInitialize]
        public void Setup()
        {
            _mockRepository = new Mock<IMaintainerRepository>();
            _sut = new GetMaintainerProfileHandler(_mockRepository.Object, NullLogger<GetMaintainerProfileHandler>.Instance);
        }

        [TestMethod]
        public async Task Handle_UserIsMaintainer_ReturnsTheProfile()
        {
            var maintainer = Maintainer.Create("user-1", "octocat", "https://avatars.githubusercontent.com/u/1");
            _mockRepository.Setup(r => r.GetByPlatformUserIdAsync("user-1")).ReturnsAsync(maintainer);

            var result = await _sut.Handle(new GetMaintainerProfileQuery { UserId = "user-1" }, CancellationToken.None);

            result.IsSuccess.Should().BeTrue();
            result.Value.Profile.Id.Should().Be(maintainer.Id.ToString());
            result.Value.Profile.GithubLogin.Should().Be("octocat");
            result.Value.Profile.AvatarUrl.Should().Be("https://avatars.githubusercontent.com/u/1");
        }

        [TestMethod]
        public async Task Handle_UserIsNotAMaintainer_ReturnsNotFound()
        {
            _mockRepository.Setup(r => r.GetByPlatformUserIdAsync(It.IsAny<string>())).ReturnsAsync((Maintainer?)null);

            var result = await _sut.Handle(new GetMaintainerProfileQuery { UserId = "user-1" }, CancellationToken.None);

            result.IsFailure.Should().BeTrue();
            result.FailureStatusCode.Should().Be(HttpStatusCode.NotFound);
        }
    }
}