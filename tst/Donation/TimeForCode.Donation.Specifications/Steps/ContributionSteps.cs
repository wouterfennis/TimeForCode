using AwesomeAssertions;
using Moq;
using Reqnroll;
using TimeForCode.Donation.Api.Client;
using TimeForCode.Donation.Application.Interfaces;
using TimeForCode.Donation.Domain;
using TimeForCode.Donation.Specifications.TestBuilder;

namespace TimeForCode.Donation.Specifications.Steps
{
    /// <summary>Logging hours on a project and listing the logged contributions.</summary>
    [Binding]
    internal class ContributionSteps
    {
        private readonly IDonationClient _donationClient;
        private readonly DonationContext _context;
        private readonly DonationFixtures _fixtures;

        private ContributionResponse? _addContributionResult;

        public ContributionSteps(IDonationClient donationClient, DonationContext context, DonationFixtures fixtures)
        {
            _donationClient = donationClient;
            _context = context;
            _fixtures = fixtures;
        }

        [Given("The user is not yet a contributor on the time for code platform")]
        public void GivenTheUserIsNotYetAContributorOnTheTimeForCodePlatform()
        {
            _fixtures.Contributors.Contributors.Should().BeEmpty();
        }

        [Given("The user has already logged {int} hours on the project")]
        public void GivenTheUserHasAlreadyLoggedHoursOnTheProject(int hours)
        {
            _fixtures.LogHours(_fixtures.TestUserContributor(), hours);
        }

        [Given("The user has already logged {int} hours on a published project")]
        public void GivenTheUserHasAlreadyLoggedHoursOnAPublishedProject(int hours)
        {
            _fixtures.PublishActiveProject();
            _fixtures.LogHours(_fixtures.TestUserContributor(), hours);
        }

        [Given("There is no project with the given identifier")]
        public void GivenThereIsNoProjectWithTheGivenIdentifier()
        {
            _context.RegisteredProjectId = ProjectBuilder.IdFor(999);
        }

        [Given("There is a published project with logged contributions")]
        public void GivenThereIsAPublishedProjectWithLoggedContributions()
        {
            _fixtures.PublishActiveProject();
            _fixtures.LogHours(_fixtures.TestUserContributor(), 3);
            _fixtures.LogHours(_fixtures.TestUserContributor(), 2);
        }

        [Given("There is a published project with contributions logged at different moments")]
        public void GivenThereIsAPublishedProjectWithContributionsLoggedAtDifferentMoments()
        {
            _fixtures.PublishActiveProject();
            var projectId = DonationFixtures.ObjectIdOf(_context.RegisteredProjectId!);
            var contributor = _fixtures.TestUserContributor();

            // Stored latest first, so only the handler's ordering can make the list chronological
            _fixtures.Contributions.CreateAsync(Contribution.Create(projectId, contributor, 5, null, DonationFixtures.FirstLoggedAt.AddDays(10)));
            _fixtures.Contributions.CreateAsync(Contribution.Create(projectId, contributor, 1, null, DonationFixtures.FirstLoggedAt));
            _fixtures.Contributions.CreateAsync(Contribution.Create(projectId, contributor, 2, null, DonationFixtures.FirstLoggedAt.AddDays(5)));
        }

        [Given("There is a published project with a contribution that has a GitHub reference")]
        public void GivenThereIsAPublishedProjectWithAContributionThatHasAGitHubReference()
        {
            _fixtures.PublishActiveProject();
            _fixtures.Contributions.CreateAsync(Contribution.Create(
                DonationFixtures.ObjectIdOf(_context.RegisteredProjectId!),
                _fixtures.TestUserContributor(),
                3,
                new Uri("https://github.com/owner/repo/pull/12"),
                DonationFixtures.FirstLoggedAt));
        }

        [Given("Another project has logged contributions")]
        public void GivenAnotherProjectHasLoggedContributions()
        {
            var otherProject = ProjectBuilder.BuildActive(ProjectBuilder.IdFor(200));
            _fixtures.ProjectRepository
                .Setup(x => x.GetByIdAsync(otherProject.Id.ToString()))
                .ReturnsAsync(otherProject);
            _fixtures.Contributions.CreateAsync(Contribution.Create(otherProject.Id, _fixtures.TestUserContributor(), 7, null, DonationFixtures.FirstLoggedAt));
        }

        [When("^The user logs (-?\\d+) hours on the project((?: with the GitHub reference \"[^\"]*\")?)$")]
        public Task WhenTheUserLogsHoursOnTheProjectAsync(int hours, string referenceSuffix)
        {
            // The suffix is empty without a reference, otherwise ` with the GitHub reference "<url>"`
            var reference = referenceSuffix.Split('"').ElementAtOrDefault(1);
            var githubReference = reference == null ? null : new Uri(reference, UriKind.RelativeOrAbsolute);

            return _context.CaptureAsync(async () => _addContributionResult = await _donationClient.AddContributionAsync(
                _context.RegisteredProjectId ?? Constants.TestProjectId,
                new AddContributionRequest { Hours = hours, GithubReference = githubReference }));
        }

        [When("The user requests the contributions of the project( without an account)")]
        public Task WhenTheUserRequestsTheContributionsOfTheProjectAsync()
        {
            return GetContributionsAsync(_context.RegisteredProjectId ?? Constants.TestProjectId);
        }

        [When("^The user requests the contributions of the project with the identifier \"(.*)\"$")]
        public Task WhenTheUserRequestsTheContributionsOfTheProjectWithTheIdentifierAsync(string identifier)
        {
            return GetContributionsAsync(identifier);
        }

        [Then("The contribution is recorded on the time for code platform")]
        public void ThenTheContributionIsRecordedOnTheTimeForCodePlatform()
        {
            _context.LastException.Should().BeNull();
            _addContributionResult.Should().NotBeNull();
            _fixtures.Contributions.Contributions.Should().Contain(c => c.Id.ToString() == _addContributionResult!.Id);
        }

        [Then("The contribution shows {int} hours")]
        public void ThenTheContributionShowsHours(int hours)
        {
            _addContributionResult.Should().NotBeNull();
            _addContributionResult!.Hours.Should().Be(hours);
        }

        [Then("The contributions of the project add up to {int} hours")]
        public void ThenTheContributionsOfTheProjectAddUpToHours(int hours)
        {
            var projectId = DonationFixtures.ObjectIdOf(_context.RegisteredProjectId!);
            var stored = _fixtures.Contributions.Contributions.Where(c => c.ProjectId == projectId);

            stored.Sum(c => c.Hours).Should().Be(hours);
        }

        [Then("No hours are logged on the project")]
        public void ThenNoHoursAreLoggedOnTheProject()
        {
            _fixtures.Contributions.Contributions.Should().BeEmpty();
        }

        [Then("The contribution shows the GitHub login and avatar of the user")]
        public void ThenTheContributionShowsTheGitHubLoginAndAvatarOfTheUser()
        {
            _addContributionResult.Should().NotBeNull();
            _addContributionResult!.ContributorGitHubLogin.Should().Be(Constants.TestUserLogin);
            _addContributionResult.ContributorAvatarUrl.Should().Be(Constants.TestUserAvatarUrl);
        }

        [Then("The contribution includes the GitHub reference {string}")]
        public void ThenTheContributionIncludesTheGitHubReference(string reference)
        {
            _addContributionResult.Should().NotBeNull();
            _addContributionResult!.GithubReference.Should().Be(reference);
        }

        [Then("^The user is registered as (?:a|one) contributor on the time for code platform$")]
        public void ThenTheUserIsRegisteredAsOneContributorOnTheTimeForCodePlatform()
        {
            var contributor = _fixtures.Contributors.Contributors.Should().ContainSingle().Subject;
            contributor.PlatformUserId.Should().Be(Constants.TestUserId);
            contributor.GithubLogin.Should().Be(Constants.TestUserLogin);
            contributor.AvatarUrl.Should().Be(Constants.TestUserAvatarUrl);
        }

        [Then("Both contributions are linked to that contributor")]
        public void ThenBothContributionsAreLinkedToThatContributor()
        {
            var contributor = _fixtures.Contributors.Contributors.Single();

            _fixtures.Contributions.Contributions.Should().HaveCount(2);
            _fixtures.Contributions.Contributions.Should().OnlyContain(c => c.ContributorId == contributor.Id);
        }

        [Then("The contributions of the project are returned")]
        public void ThenTheContributionsOfTheProjectAreReturned()
        {
            _context.LastException.Should().BeNull();
            _context.ContributionsResult.Should().NotBeNull();
            _context.ContributionsResult!.Contributions.Should().NotBeEmpty();
        }

        [Then("{int} contributions are returned")]
        public void ThenContributionsAreReturned(int count)
        {
            _context.ContributionsResult.Should().NotBeNull();
            _context.ContributionsResult!.Contributions.Should().HaveCount(count);
        }

        [Then("Each contribution includes the hours, the contributor and the time it was logged")]
        public void ThenEachContributionIncludesTheHoursTheContributorAndTheTimeItWasLogged()
        {
            _context.ContributionsResult.Should().NotBeNull();
            _context.ContributionsResult!.Contributions.Should().OnlyContain(c =>
                c.Hours > 0
                && !string.IsNullOrEmpty(c.ContributorGitHubLogin)
                && !string.IsNullOrEmpty(c.ContributorAvatarUrl)
                && c.LoggedAt != default);
        }

        [Then("The contributions are ordered from earliest to latest")]
        public void ThenTheContributionsAreOrderedFromEarliestToLatest()
        {
            _context.ContributionsResult.Should().NotBeNull();
            _context.ContributionsResult!.Contributions.Should().HaveCount(3);
            _context.ContributionsResult.Contributions.Select(c => c.LoggedAt).Should().BeInAscendingOrder();
        }

        [Then("The contribution includes a link to the GitHub reference")]
        public void ThenTheContributionIncludesALinkToTheGitHubReference()
        {
            _context.ContributionsResult.Should().NotBeNull();
            _context.ContributionsResult!.Contributions.Single().GithubReference
                .Should().Be("https://github.com/owner/repo/pull/12");
        }

        [Then("Only the contributions of the project are returned")]
        public void ThenOnlyTheContributionsOfTheProjectAreReturned()
        {
            _context.ContributionsResult.Should().NotBeNull();
            _context.ContributionsResult!.Contributions.Should().HaveCount(2);
            _context.ContributionsResult.Contributions.Should().OnlyContain(c => c.ProjectId == _context.RegisteredProjectId);
        }

        [Then("An empty list of contributions is returned")]
        public void ThenAnEmptyListOfContributionsIsReturned()
        {
            _context.LastException.Should().BeNull();
            _context.ContributionsResult.Should().NotBeNull();
            _context.ContributionsResult!.Contributions.Should().BeEmpty();
        }

        private Task GetContributionsAsync(string projectId)
        {
            return _context.CaptureAsync(async () => _context.ContributionsResult = await _donationClient.GetContributionsAsync(projectId));
        }
    }
}