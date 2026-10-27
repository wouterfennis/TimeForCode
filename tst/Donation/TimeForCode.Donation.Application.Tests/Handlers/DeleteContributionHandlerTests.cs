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
    public class DeleteContributionHandlerTests
    {
        private static readonly ObjectId ProjectId = new("5f43a0e74b12c84f1b000001");

        private Mock<IContributionRepository> _contributionRepository = default!;
        private Mock<IContributorRepository> _contributorRepository = default!;
        private Contributor _contributor = default!;
        private Contribution _contribution = default!;
        private Mock<IDonationHoursReleaser> _releaser = default!;
        private DeleteContributionHandler _sut = default!;

        [TestInitialize]
        public void Setup()
        {
            _contributor = Contributor.Create("user-1", "octocat", "https://avatars.githubusercontent.com/u/1");
            _contribution = Contribution.Create(ProjectId, _contributor, 3, null, new DateTimeOffset(2024, 3, 1, 9, 0, 0, TimeSpan.Zero));
            _contributionRepository = new Mock<IContributionRepository>();
            _contributorRepository = new Mock<IContributorRepository>();
            _releaser = new Mock<IDonationHoursReleaser>();
            _contributorRepository.Setup(r => r.GetByIdAsync(_contributor.Id)).ReturnsAsync(_contributor);
            _contributionRepository
                .Setup(r => r.GetByIdAsync(ProjectId.ToString(), _contribution.Id.ToString(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(_contribution);

            _sut = new DeleteContributionHandler(
                _contributionRepository.Object,
                _contributorRepository.Object,
                _releaser.Object,
                NullLogger<DeleteContributionHandler>.Instance);
        }

        private DeleteContributionCommand Command(string userId = "user-1", bool isAdministrator = false) => new()
        {
            ProjectId = ProjectId.ToString(),
            ContributionId = _contribution.Id.ToString(),
            UserId = userId,
            IsAdministrator = isAdministrator
        };

        [TestMethod]
        public async Task Handle_Contributor_StoresContributionMarkedAsDeleted()
        {
            Contribution? stored = null;
            _contributionRepository.Setup(r => r.UpdateAsync(It.IsAny<Contribution>(), It.IsAny<CancellationToken>()))
                .Callback<Contribution, CancellationToken>((c, _) => stored = c)
                .Returns(Task.CompletedTask);

            var result = await _sut.Handle(Command(), CancellationToken.None);

            result.IsSuccess.Should().BeTrue();
            stored.Should().NotBeNull();
            stored!.IsDeleted.Should().BeTrue();
            stored.DeletedByUserId.Should().Be("user-1");
            stored.Hours.Should().Be(3);
        }

        [TestMethod]
        public async Task Handle_Administrator_DeletesContributionOfAnotherUser()
        {
            var result = await _sut.Handle(Command(userId: "admin-1", isAdministrator: true), CancellationToken.None);

            result.IsSuccess.Should().BeTrue();
            _contributionRepository.Verify(
                r => r.UpdateAsync(It.Is<Contribution>(c => c.DeletedByUserId == "admin-1"), It.IsAny<CancellationToken>()),
                Times.Once);
        }

        [TestMethod]
        public async Task Handle_OtherUser_ReturnsForbiddenAndStoresNothing()
        {
            var result = await _sut.Handle(Command(userId: "user-2"), CancellationToken.None);

            result.FailureStatusCode.Should().Be(System.Net.HttpStatusCode.Forbidden);
            _contributionRepository.Verify(r => r.UpdateAsync(It.IsAny<Contribution>(), It.IsAny<CancellationToken>()), Times.Never);
        }

        [TestMethod]
        public async Task Handle_Contributor_ReleasesAllHoursOfTheContributionFromDonations()
        {
            await _sut.Handle(Command(), CancellationToken.None);

            _releaser.Verify(r => r.ReleaseAsync(_contribution.Id, 3, It.IsAny<CancellationToken>()), Times.Once);
        }

        [TestMethod]
        public async Task Handle_OtherUser_ReleasesNothing()
        {
            await _sut.Handle(Command(userId: "user-2"), CancellationToken.None);

            _releaser.Verify(r => r.ReleaseAsync(It.IsAny<ObjectId>(), It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Never);
        }

        [TestMethod]
        public async Task Handle_ContributionNotFoundOrDeleted_ReturnsNotFound()
        {
            _contributionRepository
                .Setup(r => r.GetByIdAsync(ProjectId.ToString(), _contribution.Id.ToString(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((Contribution?)null);

            var result = await _sut.Handle(Command(), CancellationToken.None);

            result.FailureStatusCode.Should().Be(System.Net.HttpStatusCode.NotFound);
            _contributionRepository.Verify(r => r.UpdateAsync(It.IsAny<Contribution>(), It.IsAny<CancellationToken>()), Times.Never);
        }
    }
}