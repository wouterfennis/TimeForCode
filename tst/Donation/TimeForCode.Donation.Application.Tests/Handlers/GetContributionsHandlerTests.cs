using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using MongoDB.Bson;
using Moq;
using TimeForCode.Donation.Application.Handlers;
using TimeForCode.Donation.Application.Interfaces;
using TimeForCode.Donation.Commands;
using TimeForCode.Donation.Domain;
using TimeForCode.Donation.Values;

namespace TimeForCode.Donation.Application.Tests.Handlers
{
    [TestClass]
    public class GetContributionsHandlerTests
    {
        private static readonly ObjectId ProjectId = new("5f43a0e74b12c84f1b000001");

        private Mock<IProjectRepository> _projectRepository = default!;
        private Mock<IContributionRepository> _contributionRepository = default!;
        private GetContributionsHandler _sut = default!;

        [TestInitialize]
        public void Setup()
        {
            _projectRepository = new Mock<IProjectRepository>();
            _contributionRepository = new Mock<IContributionRepository>();
            _sut = new GetContributionsHandler(
                _projectRepository.Object,
                _contributionRepository.Object,
                NullLogger<GetContributionsHandler>.Instance);
        }

        private static GetContributionsQuery Query() => new() { ProjectId = ProjectId.ToString() };

        private void GivenProject(ProjectStatus status)
        {
            _projectRepository.Setup(r => r.GetByIdAsync(ProjectId.ToString()))
                .ReturnsAsync(ProjectTestData.Build(status, "owner"));
        }

        private static Contribution Contribution(int hours, DateTimeOffset loggedAt, Uri? reference = null) => new()
        {
            Id = ObjectId.GenerateNewId(),
            ProjectId = ProjectId,
            ContributorId = new ObjectId("5f43a0e74b12c84f1b0000c1"),
            ContributorGitHubLogin = "octocat",
            ContributorAvatarUrl = "https://avatars.githubusercontent.com/u/1",
            Hours = hours,
            GithubReference = reference,
            LoggedAt = loggedAt
        };

        [TestMethod]
        public async Task Handle_Contributions_KeepsRepositoryOrder()
        {
            GivenProject(ProjectStatus.Active);
            var first = Contribution(2, new DateTimeOffset(2024, 3, 1, 0, 0, 0, TimeSpan.Zero));
            var second = Contribution(3, new DateTimeOffset(2024, 1, 1, 0, 0, 0, TimeSpan.Zero));
            _contributionRepository.Setup(r => r.GetByProjectIdAsync(ProjectId.ToString(), It.IsAny<CancellationToken>()))
                .ReturnsAsync([first, second]);

            var result = await _sut.Handle(Query(), CancellationToken.None);

            result.Value.Contributions.Select(c => c.Hours).Should().Equal(2, 3);
        }

        [TestMethod]
        public async Task Handle_ContributionWithReference_MapsAllFields()
        {
            GivenProject(ProjectStatus.Active);
            var contribution = Contribution(2, new DateTimeOffset(2024, 1, 1, 0, 0, 0, TimeSpan.Zero), new Uri("https://github.com/owner/repo/pull/12"));
            _contributionRepository.Setup(r => r.GetByProjectIdAsync(ProjectId.ToString(), It.IsAny<CancellationToken>()))
                .ReturnsAsync([contribution]);

            var result = await _sut.Handle(Query(), CancellationToken.None);

            var dto = result.Value.Contributions.Single();
            dto.Id.Should().Be(contribution.Id.ToString());
            dto.ContributorId.Should().Be(contribution.ContributorId.ToString());
            dto.ContributorGitHubLogin.Should().Be("octocat");
            dto.ContributorAvatarUrl.Should().Be("https://avatars.githubusercontent.com/u/1");
            dto.GithubReference.Should().Be("https://github.com/owner/repo/pull/12");
            dto.LoggedAt.Should().Be(contribution.LoggedAt);
        }

        [TestMethod]
        public async Task Handle_NoContributions_ReturnsEmptyList()
        {
            GivenProject(ProjectStatus.Active);
            _contributionRepository.Setup(r => r.GetByProjectIdAsync(ProjectId.ToString(), It.IsAny<CancellationToken>()))
                .ReturnsAsync([]);

            var result = await _sut.Handle(Query(), CancellationToken.None);

            result.IsSuccess.Should().BeTrue();
            result.Value.Contributions.Should().BeEmpty();
        }

        [TestMethod]
        public async Task Handle_ProjectNotFound_ReturnsFailure()
        {
            _projectRepository.Setup(r => r.GetByIdAsync(It.IsAny<string>())).ReturnsAsync((Project?)null);

            var result = await _sut.Handle(Query(), CancellationToken.None);

            result.IsFailure.Should().BeTrue();
            result.ErrorMessage.Should().Be("Project not found.");
        }

        [TestMethod]
        public async Task Handle_ProjectNotPublished_ReturnsFailure()
        {
            GivenProject(ProjectStatus.Archived);

            var result = await _sut.Handle(Query(), CancellationToken.None);

            result.IsFailure.Should().BeTrue();
        }
    }
}