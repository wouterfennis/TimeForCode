using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using MongoDB.Bson;
using Moq;
using TimeForCode.Donation.Application.Exceptions;
using TimeForCode.Donation.Application.Handlers;
using TimeForCode.Donation.Application.Interfaces;
using TimeForCode.Donation.Commands;
using TimeForCode.Donation.Domain;
using TimeForCode.Donation.Values;

namespace TimeForCode.Donation.Application.Tests.Handlers
{
    [TestClass]
    public class AddContributionHandlerTests
    {
        private static readonly ObjectId ProjectId = new("5f43a0e74b12c84f1b000001");
        private static readonly ObjectId ContributorId = new("5f43a0e74b12c84f1b0000c1");

        private Mock<IProjectRepository> _projectRepository = default!;
        private Mock<IContributorRepository> _contributorRepository = default!;
        private Mock<IContributionRepository> _contributionRepository = default!;
        private Mock<IDonationRepository> _donationRepository = default!;
        private AddContributionHandler _sut = default!;

        [TestInitialize]
        public void Setup()
        {
            _projectRepository = new Mock<IProjectRepository>();
            _contributorRepository = new Mock<IContributorRepository>();
            _contributionRepository = new Mock<IContributionRepository>();
            _donationRepository = new Mock<IDonationRepository>();
            _donationRepository.Setup(r => r.GetActiveByProjectIdAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync([]);
            _contributorRepository.Setup(r => r.UpsertAsync(It.IsAny<Contributor>()))
                .ReturnsAsync((Contributor c) => new Contributor
                {
                    Id = ContributorId,
                    PlatformUserId = c.PlatformUserId,
                    GithubLogin = c.GithubLogin,
                    AvatarUrl = c.AvatarUrl
                });

            _sut = new AddContributionHandler(
                _projectRepository.Object,
                _contributorRepository.Object,
                _contributionRepository.Object,
                _donationRepository.Object,
                NullLogger<AddContributionHandler>.Instance);
        }

        private static AddContributionCommand Valid(Uri? githubReference = null) => new()
        {
            ProjectId = ProjectId.ToString(),
            UserId = "user-1",
            GithubLogin = "octocat",
            AvatarUrl = "https://avatars.githubusercontent.com/u/1",
            Hours = 3,
            GithubReference = githubReference
        };

        private void GivenProject(ProjectStatus status)
        {
            _projectRepository.Setup(r => r.GetByIdAsync(ProjectId.ToString()))
                .ReturnsAsync(ProjectTestData.Build(status, "owner"));
        }

        [TestMethod]
        public async Task Handle_PublishedProject_StoresContributionLinkedToUpsertedContributor()
        {
            GivenProject(ProjectStatus.Active);
            Contribution? stored = null;
            _contributionRepository.Setup(r => r.CreateAsync(It.IsAny<Contribution>()))
                .Callback<Contribution>(c => stored = c)
                .Returns(Task.CompletedTask);

            var result = await _sut.Handle(Valid(), CancellationToken.None);

            result.IsSuccess.Should().BeTrue();
            stored.Should().NotBeNull();
            stored!.ProjectId.Should().Be(ProjectId);
            stored.ContributorId.Should().Be(ContributorId);
            stored.ContributorGitHubLogin.Should().Be("octocat");
            stored.ContributorAvatarUrl.Should().Be("https://avatars.githubusercontent.com/u/1");
            stored.Hours.Should().Be(3);
            stored.LoggedAt.Should().BeCloseTo(DateTimeOffset.UtcNow, TimeSpan.FromMinutes(1));
            result.Value.Contribution.Id.Should().Be(stored.Id.ToString());
            result.Value.Contribution.ContributorId.Should().Be(ContributorId.ToString());
            result.Value.Contribution.Hours.Should().Be(3);
        }

        [TestMethod]
        public async Task Handle_PublishedProject_UpsertsContributorByPlatformUserId()
        {
            GivenProject(ProjectStatus.Active);

            await _sut.Handle(Valid(), CancellationToken.None);

            _contributorRepository.Verify(r => r.UpsertAsync(It.Is<Contributor>(c =>
                c.PlatformUserId == "user-1" && c.GithubLogin == "octocat")), Times.Once);
        }

        [TestMethod]
        public async Task Handle_WithGithubReference_StoresTheReference()
        {
            GivenProject(ProjectStatus.Active);
            var reference = new Uri("https://github.com/owner/repo/pull/12");

            var result = await _sut.Handle(Valid(reference), CancellationToken.None);

            result.Value.Contribution.GithubReference.Should().Be("https://github.com/owner/repo/pull/12");
            _contributionRepository.Verify(r => r.CreateAsync(It.Is<Contribution>(c => c.GithubReference == reference)), Times.Once);
        }

        [TestMethod]
        public async Task Handle_ActiveDonation_CountsLoggedHoursAndSavesIt()
        {
            GivenProject(ProjectStatus.Active);
            var donation = DonationTestData.Build(DonationStatus.Active, committed: 5);
            _donationRepository.Setup(r => r.GetActiveByProjectIdAsync(ProjectId.ToString(), It.IsAny<CancellationToken>()))
                .ReturnsAsync([donation]);

            await _sut.Handle(Valid(), CancellationToken.None);

            donation.HoursLogged.Should().Be(3);
            donation.Status.Should().Be(DonationStatus.Active);
            _donationRepository.Verify(r => r.UpdateAsync(donation), Times.Once);
        }

        [TestMethod]
        public async Task Handle_HoursReachCommitment_CompletesDonation()
        {
            GivenProject(ProjectStatus.Active);
            var donation = DonationTestData.Build(DonationStatus.Active, committed: 3);
            _donationRepository.Setup(r => r.GetActiveByProjectIdAsync(ProjectId.ToString(), It.IsAny<CancellationToken>()))
                .ReturnsAsync([donation]);

            await _sut.Handle(Valid(), CancellationToken.None);

            donation.Status.Should().Be(DonationStatus.Completed);
        }

        [TestMethod]
        public async Task Handle_MoreHoursThanOneDonationNeeds_SpillsOverToTheNextActiveDonation()
        {
            GivenProject(ProjectStatus.Active);
            var first = DonationTestData.Build(DonationStatus.Active, committed: 2);
            var second = DonationTestData.Build(DonationStatus.Active, committed: 5);
            _donationRepository.Setup(r => r.GetActiveByProjectIdAsync(ProjectId.ToString(), It.IsAny<CancellationToken>()))
                .ReturnsAsync([first, second]);

            await _sut.Handle(Valid(), CancellationToken.None);

            first.Status.Should().Be(DonationStatus.Completed);
            second.HoursLogged.Should().Be(1);
        }

        [TestMethod]
        public async Task Handle_PendingDonation_IsLeftUntouched()
        {
            GivenProject(ProjectStatus.Active);
            var donation = DonationTestData.Build(DonationStatus.Pending, committed: 5);
            _donationRepository.Setup(r => r.GetActiveByProjectIdAsync(ProjectId.ToString(), It.IsAny<CancellationToken>()))
                .ReturnsAsync([donation]);

            await _sut.Handle(Valid(), CancellationToken.None);

            donation.HoursLogged.Should().Be(0);
            _donationRepository.Verify(r => r.UpdateAsync(It.IsAny<TimeForCode.Donation.Domain.Donation>()), Times.Never);
        }

        [TestMethod]
        public async Task Handle_ConcurrentChangeOfDonation_ReloadsAndCountsTheHours()
        {
            GivenProject(ProjectStatus.Active);
            var stale = DonationTestData.Build(DonationStatus.Active, committed: 5);
            var fresh = DonationTestData.Build(DonationStatus.Active, committed: 5);
            _donationRepository.SetupSequence(r => r.GetActiveByProjectIdAsync(ProjectId.ToString(), It.IsAny<CancellationToken>()))
                .ReturnsAsync([stale])
                .ReturnsAsync([fresh]);
            _donationRepository.SetupSequence(r => r.UpdateAsync(It.IsAny<TimeForCode.Donation.Domain.Donation>()))
                .ThrowsAsync(new RepositoryConflictException("changed"))
                .Returns(Task.CompletedTask);

            var result = await _sut.Handle(Valid(), CancellationToken.None);

            result.IsSuccess.Should().BeTrue();
            fresh.HoursLogged.Should().Be(3);
            _donationRepository.Verify(r => r.UpdateAsync(fresh), Times.Once);
        }

        [TestMethod]
        public async Task Handle_DonationKeepsChanging_StillRecordsTheContribution()
        {
            GivenProject(ProjectStatus.Active);
            _donationRepository.Setup(r => r.GetActiveByProjectIdAsync(ProjectId.ToString(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(() => [DonationTestData.Build(DonationStatus.Active, committed: 5)]);
            _donationRepository.Setup(r => r.UpdateAsync(It.IsAny<TimeForCode.Donation.Domain.Donation>()))
                .ThrowsAsync(new RepositoryConflictException("changed"));

            var result = await _sut.Handle(Valid(), CancellationToken.None);

            result.IsSuccess.Should().BeTrue();
            _contributionRepository.Verify(r => r.CreateAsync(It.IsAny<Contribution>()), Times.Once);
            _donationRepository.Verify(r => r.UpdateAsync(It.IsAny<TimeForCode.Donation.Domain.Donation>()), Times.Exactly(3));
        }

        [TestMethod]
        public async Task Handle_ProjectNotFound_ReturnsNotFound()
        {
            _projectRepository.Setup(r => r.GetByIdAsync(It.IsAny<string>())).ReturnsAsync((Project?)null);

            var result = await _sut.Handle(Valid(), CancellationToken.None);

            result.FailureStatusCode.Should().Be(System.Net.HttpStatusCode.NotFound);
        }

        [TestMethod]
        public async Task Handle_ProjectNotFound_ReturnsFailureAndStoresNothing()
        {
            _projectRepository.Setup(r => r.GetByIdAsync(It.IsAny<string>())).ReturnsAsync((Project?)null);

            var result = await _sut.Handle(Valid(), CancellationToken.None);

            result.IsFailure.Should().BeTrue();
            result.ErrorMessage.Should().Be("Project not found.");
            _contributorRepository.Verify(r => r.UpsertAsync(It.IsAny<Contributor>()), Times.Never);
            _contributionRepository.Verify(r => r.CreateAsync(It.IsAny<Contribution>()), Times.Never);
        }

        [TestMethod]
        public async Task Handle_ProjectNotPublished_ReturnsFailureAndStoresNothing()
        {
            GivenProject(ProjectStatus.Draft);

            var result = await _sut.Handle(Valid(), CancellationToken.None);

            result.IsFailure.Should().BeTrue();
            _contributionRepository.Verify(r => r.CreateAsync(It.IsAny<Contribution>()), Times.Never);
        }
    }
}