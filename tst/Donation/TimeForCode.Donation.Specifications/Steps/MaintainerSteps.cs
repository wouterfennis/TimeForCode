using FluentAssertions;
using Reqnroll;
using TimeForCode.Donation.Api.Client;
using TimeForCode.Donation.Domain;
using TimeForCode.Donation.Specifications.TestBuilder;
using ProjectStatus = TimeForCode.Donation.Values.ProjectStatus;

namespace TimeForCode.Donation.Specifications.Steps
{
    /// <summary>The maintainer profile of the user and the projects the user maintains.</summary>
    [Binding]
    internal class MaintainerSteps
    {
        private readonly IDonationClient _donationClient;
        private readonly DonationContext _context;
        private readonly DonationFixtures _fixtures;

        private MaintainerProfileResponse? _maintainerProfile;

        public MaintainerSteps(IDonationClient donationClient, DonationContext context, DonationFixtures fixtures)
        {
            _donationClient = donationClient;
            _context = context;
            _fixtures = fixtures;
        }

        [Given("The user is not yet a maintainer on the time for code platform")]
        public void GivenTheUserIsNotYetAMaintainerOnTheTimeForCodePlatform()
        {
            _fixtures.Maintainers.Maintainers.Should().BeEmpty();
        }

        [Given("The user is registered as a maintainer on the time for code platform")]
        public void GivenTheUserIsRegisteredAsAMaintainerOnTheTimeForCodePlatform()
        {
            _fixtures.Maintainers.Add(Maintainer.Create(Constants.TestUserId, Constants.TestUserLogin, Constants.TestUserAvatarUrl));
        }

        [Given("The user has an active project with donations of {int} and {int} hours")]
        public void GivenTheUserHasAnActiveProjectWithDonationsOfAndHours(int first, int second)
        {
            _fixtures.SetupLifecycleProject(ProjectStatus.Active);
            var projectId = _context.LifecycleProject!.Id;

            _fixtures.Donations.CreateAsync(DonationBuilder.Build(projectId, Values.DonationStatus.Active, "donor-1", first, 0));
            _fixtures.Donations.CreateAsync(DonationBuilder.Build(projectId, Values.DonationStatus.Active, "donor-2", second, 0));
        }

        [When("The user requests their own maintainer profile")]
        public Task WhenTheUserRequestsTheirOwnMaintainerProfileAsync()
        {
            return _context.CaptureAsync(async () => _maintainerProfile = await _donationClient.GetMyMaintainerProfileAsync());
        }

        [When("The user requests their own projects")]
        public Task WhenTheUserRequestsTheirOwnProjectsAsync()
        {
            _fixtures.SetupProjectListing();
            return _context.CaptureAsync(async () => _context.MyProjects = await _donationClient.GetMyProjectsAsync());
        }

        [Then("The user is registered as a maintainer on the time for code platform")]
        public void ThenTheUserIsRegisteredAsAMaintainerOnTheTimeForCodePlatform()
        {
            _context.LastException.Should().BeNull();
            _fixtures.Maintainers.Maintainers.Should().ContainSingle(m =>
                m.PlatformUserId == Constants.TestUserId
                && m.GithubLogin == Constants.TestUserLogin
                && m.AvatarUrl == Constants.TestUserAvatarUrl);
        }

        [Then("The user is registered as one maintainer on the time for code platform")]
        public void ThenTheUserIsRegisteredAsOneMaintainerOnTheTimeForCodePlatform()
        {
            _context.LastException.Should().BeNull();
            _fixtures.Maintainers.Maintainers.Count(m => m.PlatformUserId == Constants.TestUserId).Should().Be(1);
        }

        [Then("The maintainer profile of the user is returned")]
        public void ThenTheMaintainerProfileOfTheUserIsReturned()
        {
            _context.LastException.Should().BeNull();
            _maintainerProfile.Should().NotBeNull();

            var stored = _fixtures.Maintainers.Maintainers.Single(m => m.PlatformUserId == Constants.TestUserId);
            _maintainerProfile!.Id.Should().Be(stored.Id.ToString());
            _maintainerProfile.GithubLogin.Should().Be(Constants.TestUserLogin);
            _maintainerProfile.AvatarUrl.Should().Be(Constants.TestUserAvatarUrl);
        }

        [Then("The projects of the user are returned")]
        public void ThenTheProjectsOfTheUserAreReturned()
        {
            _context.LastException.Should().BeNull();
            _context.MyProjects.Should().NotBeNull();
            _context.MyProjects!.Projects.Should().NotBeEmpty();
        }

        [Then("The user sees the active project with {int} hours donated")]
        public void ThenTheUserSeesTheActiveProjectWithHoursDonated(int hours)
        {
            _context.MyProjects.Should().NotBeNull();
            _context.MyProjects!.Projects.Should().ContainSingle(p => p.Project.Status.ToString() == ProjectStatus.Active.ToString() && p.HoursDonated == hours);
        }

        [Then("Only the projects of the user are returned")]
        public void ThenOnlyTheProjectsOfTheUserAreReturned()
        {
            _context.LastException.Should().BeNull();
            _context.MyProjects.Should().NotBeNull();

            var ownIds = _context.KnownProjects.Where(p => p.PublishedByUserId == Constants.TestUserId).Select(p => p.Id.ToString());
            _context.MyProjects!.Projects.Select(p => p.Project.Id).Should().BeEquivalentTo(ownIds);
            _context.MyProjects.Projects.Should().NotBeEmpty();
        }
    }
}