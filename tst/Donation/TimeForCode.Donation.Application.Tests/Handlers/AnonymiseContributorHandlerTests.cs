using AwesomeAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using MongoDB.Bson;
using Moq;
using TimeForCode.Donation.Application.Handlers;
using TimeForCode.Donation.Application.Interfaces;
using TimeForCode.Donation.Commands;
using TimeForCode.Donation.Domain;

namespace TimeForCode.Donation.Application.Tests.Handlers
{
    [TestClass]
    public class AnonymiseContributorHandlerTests
    {
        private const string UserId = "user-1";
        private static readonly ObjectId ContributorId = new("5f43a0e74b12c84f1b0000c1");

        private Mock<IContributorRepository> _contributorRepository = default!;
        private Mock<IContributionRepository> _contributionRepository = default!;
        private AnonymiseContributorHandler _sut = default!;

        [TestInitialize]
        public void Setup()
        {
            _contributorRepository = new Mock<IContributorRepository>();
            _contributionRepository = new Mock<IContributionRepository>();

            _sut = new AnonymiseContributorHandler(
                _contributorRepository.Object,
                _contributionRepository.Object,
                NullLogger<AnonymiseContributorHandler>.Instance);
        }

        private void GivenContributor()
        {
            _contributorRepository.Setup(r => r.GetByPlatformUserIdAsync(UserId))
                .ReturnsAsync(new Contributor
                {
                    Id = ContributorId,
                    PlatformUserId = UserId,
                    GithubLogin = "octocat",
                    AvatarUrl = "https://avatars.githubusercontent.com/u/1"
                });
        }

        [TestMethod]
        public async Task Handle_ExistingContributor_AnonymisesContributionsAndContributor()
        {
            GivenContributor();

            var result = await _sut.Handle(new AnonymiseContributorCommand { UserId = UserId }, CancellationToken.None);

            result.IsSuccess.Should().BeTrue();
            _contributionRepository.Verify(r => r.AnonymiseByContributorIdAsync(ContributorId, It.IsAny<CancellationToken>()), Times.Once);
            _contributorRepository.Verify(r => r.AnonymiseAsync(ContributorId, It.IsAny<CancellationToken>()), Times.Once);
        }

        [TestMethod]
        public async Task Handle_ContributionsFail_DoesNotAnonymiseContributorSoRetryCanFindIt()
        {
            GivenContributor();
            _contributionRepository.Setup(r => r.AnonymiseByContributorIdAsync(ContributorId, It.IsAny<CancellationToken>()))
                .ThrowsAsync(new InvalidOperationException("database unavailable"));

            var act = () => _sut.Handle(new AnonymiseContributorCommand { UserId = UserId }, CancellationToken.None);

            await act.Should().ThrowAsync<InvalidOperationException>();
            _contributorRepository.Verify(r => r.AnonymiseAsync(It.IsAny<ObjectId>(), It.IsAny<CancellationToken>()), Times.Never);
        }

        [TestMethod]
        public async Task Handle_UnknownContributor_SucceedsWithoutChangingAnything()
        {
            _contributorRepository.Setup(r => r.GetByPlatformUserIdAsync(UserId))
                .ReturnsAsync((Contributor?)null);

            var result = await _sut.Handle(new AnonymiseContributorCommand { UserId = UserId }, CancellationToken.None);

            result.IsSuccess.Should().BeTrue();
            _contributionRepository.Verify(r => r.AnonymiseByContributorIdAsync(It.IsAny<ObjectId>(), It.IsAny<CancellationToken>()), Times.Never);
            _contributorRepository.Verify(r => r.AnonymiseAsync(It.IsAny<ObjectId>(), It.IsAny<CancellationToken>()), Times.Never);
        }
    }
}