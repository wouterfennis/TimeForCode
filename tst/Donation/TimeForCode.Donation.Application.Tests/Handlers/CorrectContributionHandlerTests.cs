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
    public class CorrectContributionHandlerTests
    {
        private static readonly ObjectId ProjectId = new("5f43a0e74b12c84f1b000001");
        private static readonly Uri Reference = new("https://github.com/owner/repo/pull/12");

        private Mock<IContributionRepository> _contributionRepository = default!;
        private Mock<IContributorRepository> _contributorRepository = default!;
        private Contributor _contributor = default!;
        private Contribution _contribution = default!;
        private Mock<IDonationHoursReleaser> _releaser = default!;
        private Mock<IDonationHoursCounter> _counter = default!;
        private CorrectContributionHandler _sut = default!;

        [TestInitialize]
        public void Setup()
        {
            _contributor = Contributor.Create("user-1", "octocat", "https://avatars.githubusercontent.com/u/1");
            _contribution = Contribution.Create(ProjectId, _contributor, 3, Reference, new DateTimeOffset(2024, 3, 1, 9, 0, 0, TimeSpan.Zero));
            _contributionRepository = new Mock<IContributionRepository>();
            _contributorRepository = new Mock<IContributorRepository>();
            _releaser = new Mock<IDonationHoursReleaser>();
            _counter = new Mock<IDonationHoursCounter>();
            _contributorRepository.Setup(r => r.GetByIdAsync(_contributor.Id)).ReturnsAsync(_contributor);
            _contributionRepository
                .Setup(r => r.GetByIdAsync(ProjectId.ToString(), _contribution.Id.ToString(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(_contribution);

            _sut = new CorrectContributionHandler(
                _contributionRepository.Object,
                _contributorRepository.Object,
                _counter.Object,
                _releaser.Object,
                NullLogger<CorrectContributionHandler>.Instance);
        }

        private CorrectContributionCommand Command(string userId = "user-1", bool isAdministrator = false, int hours = 5, Uri? reference = null) => new()
        {
            ProjectId = ProjectId.ToString(),
            ContributionId = _contribution.Id.ToString(),
            UserId = userId,
            IsAdministrator = isAdministrator,
            Hours = hours,
            GithubReference = reference
        };

        [TestMethod]
        public async Task Handle_Contributor_StoresCorrectedContributionWithHistory()
        {
            Contribution? stored = null;
            _contributionRepository.Setup(r => r.UpdateAsync(It.IsAny<Contribution>(), It.IsAny<CancellationToken>()))
                .Callback<Contribution, CancellationToken>((c, _) => stored = c)
                .Returns(Task.CompletedTask);

            var result = await _sut.Handle(Command(), CancellationToken.None);

            result.IsSuccess.Should().BeTrue();
            stored.Should().NotBeNull();
            stored!.Hours.Should().Be(5);
            stored.GithubReference.Should().BeNull();
            stored.History.Should().ContainSingle().Which.Hours.Should().Be(3);
            result.Value.Contribution.Hours.Should().Be(5);
        }

        [TestMethod]
        public async Task Handle_Administrator_CorrectsContributionOfAnotherUser()
        {
            var result = await _sut.Handle(Command(userId: "admin-1", isAdministrator: true), CancellationToken.None);

            result.IsSuccess.Should().BeTrue();
            _contributionRepository.Verify(
                r => r.UpdateAsync(It.Is<Contribution>(c => c.History.Single().CorrectedByUserId == "admin-1"), It.IsAny<CancellationToken>()),
                Times.Once);
        }

        [TestMethod]
        public async Task Handle_OtherUser_ReturnsForbiddenAndStoresNothing()
        {
            var result = await _sut.Handle(Command(userId: "user-2"), CancellationToken.None);

            result.IsFailure.Should().BeTrue();
            result.FailureStatusCode.Should().Be(System.Net.HttpStatusCode.Forbidden);
            _contributionRepository.Verify(r => r.UpdateAsync(It.IsAny<Contribution>(), It.IsAny<CancellationToken>()), Times.Never);
        }

        [TestMethod]
        public async Task Handle_HoursLowered_ReleasesTheDifferenceFromDonations()
        {
            await _sut.Handle(Command(hours: 1), CancellationToken.None);

            _releaser.Verify(r => r.ReleaseAsync(_contribution.Id, 2, It.IsAny<CancellationToken>()), Times.Once);
        }

        [TestMethod]
        public async Task Handle_HoursRaised_CountsTheExtraHoursTowardDonations()
        {
            await _sut.Handle(Command(hours: 5), CancellationToken.None);

            _counter.Verify(c => c.CountAsync(ProjectId.ToString(), _contribution.Id, 2, It.IsAny<CancellationToken>()), Times.Once);
        }

        [TestMethod]
        [DataRow(1)]
        [DataRow(3)]
        public async Task Handle_HoursSameOrLowered_CountsNothing(int hours)
        {
            await _sut.Handle(Command(hours: hours), CancellationToken.None);

            _counter.Verify(c => c.CountAsync(It.IsAny<string>(), It.IsAny<ObjectId>(), It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Never);
        }

        [TestMethod]
        public async Task Handle_OtherUser_CountsNothing()
        {
            await _sut.Handle(Command(userId: "user-2", hours: 5), CancellationToken.None);

            _counter.Verify(c => c.CountAsync(It.IsAny<string>(), It.IsAny<ObjectId>(), It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Never);
        }

        [TestMethod]
        [DataRow(3)]
        [DataRow(5)]
        public async Task Handle_HoursSameOrRaised_ReleasesNothing(int hours)
        {
            await _sut.Handle(Command(hours: hours), CancellationToken.None);

            _releaser.Verify(r => r.ReleaseAsync(It.IsAny<ObjectId>(), It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Never);
        }

        [TestMethod]
        public async Task Handle_OtherUser_ReleasesNothing()
        {
            await _sut.Handle(Command(userId: "user-2", hours: 1), CancellationToken.None);

            _releaser.Verify(r => r.ReleaseAsync(It.IsAny<ObjectId>(), It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Never);
        }

        [TestMethod]
        public async Task Handle_ContributionNotFoundOrDeleted_ReturnsNotFound()
        {
            _contributionRepository
                .Setup(r => r.GetByIdAsync(ProjectId.ToString(), _contribution.Id.ToString(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((Contribution?)null);

            var result = await _sut.Handle(Command(), CancellationToken.None);

            result.IsFailure.Should().BeTrue();
            result.FailureStatusCode.Should().Be(System.Net.HttpStatusCode.NotFound);
            _contributionRepository.Verify(r => r.UpdateAsync(It.IsAny<Contribution>(), It.IsAny<CancellationToken>()), Times.Never);
        }

        [TestMethod]
        public async Task Handle_ContributorAnonymisedAndCallerNotAdministrator_ReturnsForbidden()
        {
            _contributorRepository.Setup(r => r.GetByIdAsync(_contributor.Id)).ReturnsAsync((Contributor?)null);

            var result = await _sut.Handle(Command(), CancellationToken.None);

            result.FailureStatusCode.Should().Be(System.Net.HttpStatusCode.Forbidden);
        }
    }
}