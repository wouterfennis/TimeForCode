using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using MongoDB.Bson;
using Moq;
using Reqnroll;
using TimeForCode.Donation.Api.Client;
using TimeForCode.Donation.Application.Interfaces;
using TimeForCode.Donation.Domain;
using TimeForCode.Donation.Specifications.Mocking;
using TimeForCode.Donation.Specifications.TestBuilder;

namespace TimeForCode.Donation.Specifications.Steps
{
    internal partial class ProjectSteps
    {
        private static readonly DateTimeOffset FirstLoggedAt = new(2024, 2, 1, 9, 0, 0, TimeSpan.Zero);

        private ContributionResponse? _addContributionResult;
        private GetContributionsResponse? _getContributionsResult;

        private InMemoryContributorRepository Contributors => _provider.GetRequiredService<InMemoryContributorRepository>();
        private InMemoryContributionRepository Contributions => _provider.GetRequiredService<InMemoryContributionRepository>();

        [Given("The user is not yet a contributor on the time for code platform")]
        public void GivenTheUserIsNotYetAContributorOnTheTimeForCodePlatform()
        {
            Contributors.Contributors.Should().BeEmpty();
        }

        [Given("The user has already logged {int} hours on the project")]
        public void GivenTheUserHasAlreadyLoggedHoursOnTheProject(int hours)
        {
            var contributor = TestUserContributor();
            var nextMoment = FirstLoggedAt.AddDays(Contributions.Contributions.Count);
            Contributions.CreateAsync(NewContribution(ObjectIdOf(_registeredProjectId!), contributor, hours, nextMoment));
        }

        [Given("There is no project with the given identifier")]
        public void GivenThereIsNoProjectWithTheGivenIdentifier()
        {
            _registeredProjectId = ProjectBuilder.IdFor(999);
        }

        [Given("There is a published project with logged contributions")]
        public void GivenThereIsAPublishedProjectWithLoggedContributions()
        {
            GivenThereIsAPublishedProjectOnTheTimeForCodePlatform();
            GivenTheUserHasAlreadyLoggedHoursOnTheProject(3);
            GivenTheUserHasAlreadyLoggedHoursOnTheProject(2);
        }

        [Given("There is a published project with contributions logged at different moments")]
        public void GivenThereIsAPublishedProjectWithContributionsLoggedAtDifferentMoments()
        {
            GivenThereIsAPublishedProjectOnTheTimeForCodePlatform();
            var projectId = ObjectIdOf(_registeredProjectId!);
            var contributor = TestUserContributor();

            // Stored latest first, so only the handler's ordering can make the list chronological
            Contributions.CreateAsync(NewContribution(projectId, contributor, 5, FirstLoggedAt.AddDays(10)));
            Contributions.CreateAsync(NewContribution(projectId, contributor, 1, FirstLoggedAt));
            Contributions.CreateAsync(NewContribution(projectId, contributor, 2, FirstLoggedAt.AddDays(5)));
        }

        [Given("There is a published project with a contribution that has a GitHub reference")]
        public void GivenThereIsAPublishedProjectWithAContributionThatHasAGitHubReference()
        {
            GivenThereIsAPublishedProjectOnTheTimeForCodePlatform();
            Contributions.CreateAsync(NewContribution(
                ObjectIdOf(_registeredProjectId!),
                TestUserContributor(),
                3,
                FirstLoggedAt,
                new Uri("https://github.com/owner/repo/pull/12")));
        }

        [Given("Another project has logged contributions")]
        public void GivenAnotherProjectHasLoggedContributions()
        {
            var otherProject = ProjectBuilder.BuildActive(ProjectBuilder.IdFor(200));
            _provider.GetRequiredService<Mock<IProjectRepository>>()
                .Setup(x => x.GetByIdAsync(otherProject.Id.ToString()))
                .ReturnsAsync(otherProject);
            Contributions.CreateAsync(NewContribution(otherProject.Id, TestUserContributor(), 7, FirstLoggedAt));
        }

        [When("^The user logs (-?\\d+) hours on the project((?: with the GitHub reference \"[^\"]*\")?)$")]
        public Task WhenTheUserLogsHoursOnTheProjectAsync(int hours, string referenceSuffix)
        {
            // The suffix is empty without a reference, otherwise ` with the GitHub reference "<url>"`
            var reference = referenceSuffix.Split('"').ElementAtOrDefault(1);
            var githubReference = reference == null ? null : new Uri(reference, UriKind.RelativeOrAbsolute);

            return CaptureAsync(async () => _addContributionResult = await _donationClient.AddContributionAsync(
                _registeredProjectId ?? Constants.TestProjectId,
                new AddContributionRequest { Hours = hours, GithubReference = githubReference }));
        }

        [When("The user requests the contributions of the project")]
        [When("The user requests the contributions of the project without an account")]
        public Task WhenTheUserRequestsTheContributionsOfTheProjectAsync()
        {
            return GetContributionsAsync(_registeredProjectId ?? Constants.TestProjectId);
        }

        [Then("The contribution is recorded on the time for code platform")]
        public void ThenTheContributionIsRecordedOnTheTimeForCodePlatform()
        {
            _exception.Should().BeNull();
            _addContributionResult.Should().NotBeNull();
            Contributions.Contributions.Should().Contain(c => c.Id.ToString() == _addContributionResult!.Id);
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
            var projectId = ObjectIdOf(_registeredProjectId!);
            var stored = Contributions.Contributions.Where(c => c.ProjectId == projectId);

            stored.Sum(c => c.Hours).Should().Be(hours);
        }

        [Then("No hours are logged on the project")]
        public void ThenNoHoursAreLoggedOnTheProject()
        {
            Contributions.Contributions.Should().BeEmpty();
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
            var contributor = Contributors.Contributors.Should().ContainSingle().Subject;
            contributor.PlatformUserId.Should().Be(Constants.TestUserId);
            contributor.GithubLogin.Should().Be(Constants.TestUserLogin);
            contributor.AvatarUrl.Should().Be(Constants.TestUserAvatarUrl);
        }

        [Then("Both contributions are linked to that contributor")]
        public void ThenBothContributionsAreLinkedToThatContributor()
        {
            var contributor = Contributors.Contributors.Single();

            Contributions.Contributions.Should().HaveCount(2);
            Contributions.Contributions.Should().OnlyContain(c => c.ContributorId == contributor.Id);
        }

        [Then("The contributions of the project are returned")]
        public void ThenTheContributionsOfTheProjectAreReturned()
        {
            _exception.Should().BeNull();
            _getContributionsResult.Should().NotBeNull();
            _getContributionsResult!.Contributions.Should().NotBeEmpty();
        }

        [Then("{int} contributions are returned")]
        public void ThenContributionsAreReturned(int count)
        {
            _getContributionsResult.Should().NotBeNull();
            _getContributionsResult!.Contributions.Should().HaveCount(count);
        }

        [Then("Each contribution includes the hours, the contributor and the time it was logged")]
        public void ThenEachContributionIncludesTheHoursTheContributorAndTheTimeItWasLogged()
        {
            _getContributionsResult.Should().NotBeNull();
            _getContributionsResult!.Contributions.Should().OnlyContain(c =>
                c.Hours > 0
                && !string.IsNullOrEmpty(c.ContributorGitHubLogin)
                && !string.IsNullOrEmpty(c.ContributorAvatarUrl)
                && c.LoggedAt != default);
        }

        [Then("The contributions are ordered from earliest to latest")]
        public void ThenTheContributionsAreOrderedFromEarliestToLatest()
        {
            _getContributionsResult.Should().NotBeNull();
            _getContributionsResult!.Contributions.Should().HaveCount(3);
            _getContributionsResult.Contributions.Select(c => c.LoggedAt).Should().BeInAscendingOrder();
        }

        [Then("The contribution includes a link to the GitHub reference")]
        public void ThenTheContributionIncludesALinkToTheGitHubReference()
        {
            _getContributionsResult.Should().NotBeNull();
            _getContributionsResult!.Contributions.Single().GithubReference
                .Should().Be("https://github.com/owner/repo/pull/12");
        }

        [Then("Only the contributions of the project are returned")]
        public void ThenOnlyTheContributionsOfTheProjectAreReturned()
        {
            _getContributionsResult.Should().NotBeNull();
            _getContributionsResult!.Contributions.Should().HaveCount(2);
            _getContributionsResult.Contributions.Should().OnlyContain(c => c.ProjectId == _registeredProjectId);
        }

        [Then("An empty list of contributions is returned")]
        public void ThenAnEmptyListOfContributionsIsReturned()
        {
            _exception.Should().BeNull();
            _getContributionsResult.Should().NotBeNull();
            _getContributionsResult!.Contributions.Should().BeEmpty();
        }

        private Task GetContributionsAsync(string projectId)
        {
            return CaptureAsync(async () => _getContributionsResult = await _donationClient.GetContributionsAsync(projectId));
        }

        private Contributor TestUserContributor()
        {
            var existing = Contributors.Contributors.SingleOrDefault(c => c.PlatformUserId == Constants.TestUserId);
            if (existing != null)
            {
                return existing;
            }

            var contributor = Contributor.Create(Constants.TestUserId, Constants.TestUserLogin, Constants.TestUserAvatarUrl);
            Contributors.Add(contributor);
            return contributor;
        }

        private static Contribution NewContribution(
            ObjectId projectId,
            Contributor contributor,
            int hours,
            DateTimeOffset loggedAt,
            Uri? githubReference = null)
        {
            return Contribution.Create(projectId, contributor, hours, githubReference, loggedAt);
        }

        private static ObjectId ObjectIdOf(string id)
        {
            return new ObjectId(id);
        }
    }
}