using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Reqnroll;
using TimeForCode.Donation.Api.Client;
using TimeForCode.Donation.Application.Interfaces;
using TimeForCode.Donation.Domain;
using TimeForCode.Donation.Specifications.Mocking;
using TimeForCode.Donation.Specifications.TestBuilder;
using ProjectStatus = TimeForCode.Donation.Values.ProjectStatus;

namespace TimeForCode.Donation.Specifications.Steps
{
    internal partial class ProjectSteps
    {
        private readonly List<Project> _knownProjects = [];

        private MaintainerProfileResponse? _maintainerProfile;
        private GetMyProjectsResult? _myProjects;

        private InMemoryMaintainerRepository Maintainers => _provider.GetRequiredService<InMemoryMaintainerRepository>();

        [Given("The user is not yet a maintainer on the time for code platform")]
        public void GivenTheUserIsNotYetAMaintainerOnTheTimeForCodePlatform()
        {
            Maintainers.Maintainers.Should().BeEmpty();
        }

        [Given("The user is registered as a maintainer on the time for code platform")]
        public void GivenTheUserIsRegisteredAsAMaintainerOnTheTimeForCodePlatform()
        {
            Maintainers.Add(Maintainer.Create(Constants.TestUserId, Constants.TestUserLogin, Constants.TestUserAvatarUrl));
        }

        [Given("The user has an active project with donations of {int} and {int} hours")]
        public void GivenTheUserHasAnActiveProjectWithDonationsOfAndHours(int first, int second)
        {
            SetupLifecycleProject(ProjectStatus.Active);
            var projectId = _lifecycleProject!.Id;

            Donations.CreateAsync(DonationBuilder.Build(projectId, Values.DonationStatus.Active, "donor-1", first, 0));
            Donations.CreateAsync(DonationBuilder.Build(projectId, Values.DonationStatus.Active, "donor-2", second, 0));
        }

        [When("The user requests their own maintainer profile")]
        public Task WhenTheUserRequestsTheirOwnMaintainerProfileAsync()
        {
            return CaptureAsync(async () => _maintainerProfile = await _donationClient.GetMyMaintainerProfileAsync());
        }

        [When("The user requests their own projects")]
        public Task WhenTheUserRequestsTheirOwnProjectsAsync()
        {
            SetupProjectListing();
            return CaptureAsync(async () => _myProjects = await _donationClient.GetMyProjectsAsync());
        }

        [Then("The user is registered as a maintainer on the time for code platform")]
        public void ThenTheUserIsRegisteredAsAMaintainerOnTheTimeForCodePlatform()
        {
            _exception.Should().BeNull();
            Maintainers.Maintainers.Should().ContainSingle(m =>
                m.PlatformUserId == Constants.TestUserId
                && m.GithubLogin == Constants.TestUserLogin
                && m.AvatarUrl == Constants.TestUserAvatarUrl);
        }

        [Then("The user is registered as one maintainer on the time for code platform")]
        public void ThenTheUserIsRegisteredAsOneMaintainerOnTheTimeForCodePlatform()
        {
            _exception.Should().BeNull();
            Maintainers.Maintainers.Count(m => m.PlatformUserId == Constants.TestUserId).Should().Be(1);
        }

        [Then("The maintainer profile of the user is returned")]
        public void ThenTheMaintainerProfileOfTheUserIsReturned()
        {
            _exception.Should().BeNull();
            _maintainerProfile.Should().NotBeNull();

            var stored = Maintainers.Maintainers.Single(m => m.PlatformUserId == Constants.TestUserId);
            _maintainerProfile!.Id.Should().Be(stored.Id.ToString());
            _maintainerProfile.GithubLogin.Should().Be(Constants.TestUserLogin);
            _maintainerProfile.AvatarUrl.Should().Be(Constants.TestUserAvatarUrl);
        }

        [Then("The projects of the user are returned")]
        public void ThenTheProjectsOfTheUserAreReturned()
        {
            _exception.Should().BeNull();
            _myProjects.Should().NotBeNull();
            _myProjects!.Projects.Should().NotBeEmpty();
        }

        [Then("The user sees the active project with {int} hours donated")]
        public void ThenTheUserSeesTheActiveProjectWithHoursDonated(int hours)
        {
            _myProjects.Should().NotBeNull();
            _myProjects!.Projects.Should().ContainSingle(p => p.Project.Status.ToString() == ProjectStatus.Active.ToString() && p.HoursDonated == hours);
        }

        [Then("Only the projects of the user are returned")]
        public void ThenOnlyTheProjectsOfTheUserAreReturned()
        {
            _exception.Should().BeNull();
            _myProjects.Should().NotBeNull();

            var ownIds = _knownProjects.Where(p => p.PublishedByUserId == Constants.TestUserId).Select(p => p.Id.ToString());
            _myProjects!.Projects.Select(p => p.Project.Id).Should().BeEquivalentTo(ownIds);
            _myProjects.Projects.Should().NotBeEmpty();
        }

        /// <summary>Keeps every project of the scenario so the own-projects listing is answered like the real repository: by publisher.</summary>
        private void TrackProject(Project project)
        {
            _knownProjects.Add(project);
            SetupProjectListing();
        }

        private void SetupProjectListing()
        {
            var mockProjectRepository = _provider.GetRequiredService<Mock<IProjectRepository>>();
            mockProjectRepository.Setup(x => x.GetByPublishedByUserIdAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((string userId, CancellationToken _) => (IReadOnlyList<Project>)_knownProjects.Where(p => p.PublishedByUserId == userId).ToList());
        }
    }
}